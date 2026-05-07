using System.Text.Json.Serialization;
using GdeOni.API;
using GdeOni.API.Extensions;
using GdeOni.API.Hosting;
using GdeOni.API.RateLimiting;
using GdeOni.Application;
using GdeOni.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddApplication();
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCustomCors(builder.Configuration, builder.Environment);
builder.Services.AddCustomRateLimiting(builder.Configuration);

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

var app = builder.Build();

await app.Services.SeedDatabaseAsync();
await app.Services.BootstrapStorageAsync();

// Должно идти раньше остального middleware: исправляет
// Connection.RemoteIpAddress / Scheme до того, как их прочитают
// логи запросов, CORS, аутентификация, RefreshToken.CreatedFromIp
// и т.п. Без конфигурации Hosting:KnownProxies / KnownNetworks —
// no-op (см. D7.38).
app.UseForwardedHeadersIfConfigured(builder.Configuration);

app.UseSerilogRequestLogging();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(GdeOni.API.DependencyInjection.CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

// После Authentication — лимит знает client IP (через ForwardedHeaders D7.38)
// и привязан к политике auth (D7.39).
app.UseRateLimiter();

app.MapControllers();
app.Run();

// Сделано видимым для WebApplicationFactory<Program> в integration-тестах
// (.NET 6+ top-level Program генерируется как internal partial). См. D9.4.
public partial class Program;