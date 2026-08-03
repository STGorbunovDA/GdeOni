using System.Text.Json;
using System.Text.Json.Serialization;
using GdeOni.API;
using GdeOni.API.Extensions;
using GdeOni.API.HealthChecks;
using GdeOni.API.Hosting;
using GdeOni.API.Legal;
using GdeOni.API.Middleware;
using GdeOni.API.Observability;
using GdeOni.API.Options;
using GdeOni.API.RateLimiting;
using GdeOni.Application;
using GdeOni.Application.Abstractions.Legal;
using GdeOni.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

// D21. Sentry поднимается через WebHost — это no-op, если в
// appsettings нет секции Sentry или Dsn пуст. См. SentryRegistration.
builder.AddCustomSentry();

builder.Services.AddApplication();
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCustomCors(builder.Configuration, builder.Environment);
builder.Services.AddCustomRateLimiting(builder.Configuration);

// D19.9. Тексты Privacy Policy / Terms — канонические файлы из
// backend/docs/legal, копируются в {ContentRoot}/Legal при сборке.
// Реализация в API (а не Infrastructure), т.к. зависит от ContentRoot.
// Singleton: файлы читаются один раз и кешируются в памяти.
builder.Services.AddSingleton<ILegalDocumentSource, FileLegalDocumentSource>();
builder.Services.Configure<WebhookSecurityOptions>(
    builder.Configuration.GetSection(WebhookSecurityOptions.SectionName));

// D17. Информация о версиях клиента отдаётся через /api/app/version.
// Секция в appsettings опциональна — дефолты в самом классе.
builder.Services.Configure<AppVersionOptions>(
    builder.Configuration.GetSection(AppVersionOptions.SectionName));
builder.Services.Configure<GeolocationOptions>(
    builder.Configuration.GetSection(GeolocationOptions.SectionName));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Сериализовать enum'ы строками (RelationshipType, MediaKind,
        // ModerationStatus, UserRole, TrackStatus, LocationAccuracy,
        // RoutingMode). До D11.1.1 enum уходили целыми числами,
        // что заставляло клиентов помнить ordinals.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCustomSwagger();

// D21. Health-checks: Postgres + MinIO. Эндпоинт /health
// маппится ниже как AllowAnonymous — балансировщик / liveness probe
// должен ходить без JWT.
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: new[] { "db", "ready" })
    .AddCheck<MinioHealthCheck>("minio", tags: new[] { "storage", "ready" });

var app = builder.Build();

// Fail-fast старта: проверяем что миграции накатаны. Без этого API
// стартует и падает на первом запросе с непонятной ошибкой EF Core
// ("column does not exist"). См. D11.6.
await app.Services.EnsureDatabaseMigrationsAppliedAsync();

// D19.9. Fail-fast: текст юр-документа и его версия в конфиге должны
// совпадать. Иначе юзер подтвердит согласие не с тем текстом, который
// прочитал.
app.Services.EnsureLegalDocumentsMatchVersions();

await app.Services.SeedDatabaseAsync();
await app.Services.BootstrapStorageAsync();

// Должно идти раньше остального middleware: исправляет
// Connection.RemoteIpAddress / Scheme до того, как их прочитают
// логи запросов, CORS, аутентификация, RefreshToken.CreatedFromIp
// и т.п. Без конфигурации Hosting:KnownProxies / KnownNetworks —
// no-op (см. D7.38).
app.UseForwardedHeadersIfConfigured(builder.Configuration);

// Correlation id в логах: каждый лог запроса теперь несёт TraceId
// (HTTP TraceIdentifier) и UserId (если юзер уже аутентифицирован).
// В Seq можно фильтровать "все логи запроса X" одним кликом —
// без этого диагностика инцидента превращается в распутывание клубка.
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("TraceId", http.TraceIdentifier);
        var userIdClaim = http.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim))
            diag.Set("UserId", userIdClaim);
    };
});

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(GdeOni.API.DependencyInjection.CorsPolicyName);

// IP-whitelist для webhook'ов. No-op если AllowedCidrs пуст —
// затраты ~10нс на проверку Path.StartsWithSegments. См.
// WebhookIpWhitelistMiddleware.
app.UseMiddleware<WebhookIpWhitelistMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// После Authentication — лимит знает client IP (через ForwardedHeaders D7.38)
// и привязан к политике auth (D7.39).
app.UseRateLimiter();

// D21. Health-checks для k8s probes:
//
// /health      — все проверки (Postgres + MinIO) для прод-мониторинга
//                и обратной совместимости со старой настройкой балансировщика.
// /health/live — только liveness: процесс жив, отвечает 200 без побочных
//                запросов. Используется k8s livenessProbe: при провале
//                под рестартится. БД не проверяется намеренно — если
//                Postgres временно лёг, поды виноваты не будут.
// /health/ready — readiness: реальные зависимости (Postgres + MinIO).
//                При провале под перестаёт получать трафик, но не рестартится.
//                Теги "ready" уже расставлены в AddHealthChecks выше.
//
// AllowAnonymous — балансировщик и k8s probes ходят без JWT.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthResponse,
}).AllowAnonymous();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // Predicate _ => false: пропускаем все checks, отвечаем 200 если
    // процесс жив. Этого достаточно для liveness.
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse,
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse,
}).AllowAnonymous();

app.MapControllers();
app.Run();

static async Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = new
    {
        status = report.Status.ToString(),
        totalDurationMs = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            durationMs = entry.Value.Duration.TotalMilliseconds,
            // Stack trace из exception не отдаём наружу (info-disclosure).
            error = entry.Value.Exception?.GetType().Name,
        }),
    };
    await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}

// Сделано видимым для WebApplicationFactory<Program> в integration-тестах
// (.NET 6+ top-level Program генерируется как internal partial). См. D9.4.
/// <summary>
/// Точка входа приложения. Объявлена как <c>public partial</c>, чтобы
/// <c>WebApplicationFactory&lt;Program&gt;</c> в integration-тестах
/// мог поднять in-memory хост приложения.
/// </summary>
public partial class Program;