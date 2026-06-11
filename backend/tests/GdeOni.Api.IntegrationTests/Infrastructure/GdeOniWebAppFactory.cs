using DotNet.Testcontainers.Builders;
using GdeOni.Infrastructure.Persistence;
using GdeOni.Infrastructure.Persistence.Cleanup;
using GdeOni.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;

namespace GdeOni.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Тестовый <see cref="WebApplicationFactory{Program}"/> для
/// интеграционных тестов API. Перед запуском тестов поднимает в Docker:
/// — PostgreSQL 16 (Testcontainers.PostgreSql);
/// — MinIO latest (Testcontainers.Minio);
/// и переопределяет конфигурацию приложения, чтобы оно ходило в эти
/// контейнеры, а не в реальные сервисы.
///
/// Жизненный цикл управляется через <see cref="IAsyncLifetime"/>:
/// xUnit вызывает InitializeAsync до первого теста и DisposeAsync
/// после последнего, что удерживает контейнеры на всё время прохода.
///
/// Внимание: тесты требуют установленный и запущенный Docker. Если
/// его нет — InitializeAsync упадёт ещё до того, как начнут проходить
/// тесты. Это сознательное ограничение: integration-тесты должны бить
/// настоящий Postgres и настоящий MinIO, чтобы покрывать SQL-фичи
/// (ILike, jsonb, snake_case naming) и presigned-URL логику.
/// </summary>
public sealed class GdeOniWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    static GdeOniWebAppFactory()
    {
        // Env-vars устанавливаются ДО создания WebApplicationBuilder
        // в Program.cs, поэтому Configuration snapshot, читаемый
        // синхронно в builder.Services.AddXxx(builder.Configuration),
        // увидит именно эти значения. AddInMemoryCollection через
        // ConfigureAppConfiguration работает только для отложенного
        // чтения через IOptions — для snapshot-pattern (используется
        // в AddSecurity для IssuerSigningKey и в AddCustomRateLimiting
        // для PermitLimit) нужны env-vars.
        //
        // Двойное подчёркивание — convention .NET для nested keys
        // (Jwt:Issuer → Jwt__Issuer).
        //
        // КРИТИЧНО для JWT: AddSecurity фиксирует
        // TokenValidationParameters.IssuerSigningKey ОДИН РАЗ при
        // старте через snapshot SecretKey. Если на этот момент
        // override ещё не применён — JwtBearerHandler валидирует
        // токены старым ключом, а JwtProvider (через IOptions lazy)
        // подписывает новым → "signature key was not found".
        Environment.SetEnvironmentVariable("Jwt__Issuer", "GdeOni.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "GdeOni.Tests.Client");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "test-secret-key-with-at-least-32-bytes!!");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenLifetimeMinutes", "30");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenLifetimeDays", "7");
        Environment.SetEnvironmentVariable("Jwt__SecurityStampCacheTtlSeconds", "30");

        Environment.SetEnvironmentVariable("RateLimiting__Auth__PermitLimit", "100000");
        Environment.SetEnvironmentVariable("RateLimiting__Auth__WindowMinutes", "60");
    }

    // Тот же образ, что и в backend/docker-compose.yml — чтобы локальная
    // разработка и тесты бились в одну и ту же версию Postgres.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("gdeoni_test")
        .WithUsername("gdeoni")
        .WithPassword("gdeoni_test_pwd")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
        .Build();

    private readonly MinioContainer _minio = new MinioBuilder()
        .WithImage("minio/minio:latest")
        .WithUsername("minioadmin")
        .WithPassword("minioadmin")
        .Build();

    /// <summary>
    /// Точка переопределения хост-конфигурации: туда мы подкладываем
    /// connection string'и от Testcontainers и заведомо валидные
    /// JWT-настройки. Делается ДО Build(), чтобы AddSecurity / AddInfrastructure
    /// прочитали именно эти значения, а не локальный appsettings.json.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development — чтобы AddCustomCors не упал fail-fast'ом
        // (там Production требует Cors:AllowedOrigins; см. D7.64).
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration(config =>
        {
            // Сбрасываем источники из реального хост-проекта, чтобы
            // не подмешивать appsettings.Development.json разработчика.
            // Конфигурацию выдаём целиком in-memory.
            config.Sources.Clear();

            var minioEndpoint = new Uri(_minio.GetConnectionString())
                .Authority; // host:port без схемы — формат, который ждёт MinIO SDK.

            var inMemory = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),

                // JWT: SecretKey строго ≥32 байта (D7.65 fail-fast при старте).
                ["Jwt:Issuer"] = "GdeOni.Tests",
                ["Jwt:Audience"] = "GdeOni.Tests.Client",
                ["Jwt:SecretKey"] = "test-secret-key-with-at-least-32-bytes!!",
                ["Jwt:AccessTokenLifetimeMinutes"] = "30",
                ["Jwt:RefreshTokenLifetimeDays"] = "7",
                ["Jwt:SecurityStampCacheTtlSeconds"] = "30",

                // MinIO под Testcontainers — без TLS, дефолтные buckets.
                ["Minio:Endpoint"] = minioEndpoint,
                ["Minio:AccessKey"] = "minioadmin",
                ["Minio:SecretKey"] = "minioadmin",
                ["Minio:UseSsl"] = "false",
                ["Minio:Buckets:DeceasedPhotos"] = "deceased-photos",
                ["Minio:Buckets:GravePhotos"] = "grave-photos",
                ["Minio:Buckets:DeceasedDocuments"] = "deceased-documents",
                // Cleanup отключаем: фоновый сервис гоняет периодический
                // proc, который в тестах создаёт шум и flaky-fail'ы.
                ["Minio:Cleanup:Enabled"] = "false",

                // RefreshToken cleanup тоже отключаем по той же причине.
                ["RefreshTokensCleanup:Enabled"] = "false",

                // Cors: одно фейковое origin, чтобы AddCustomCors прошёл
                // ветку "configured origins" без development-fallback'а.
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",

                // Rate-limit: дефолтные 10 req/min на IP убивают коллекцию
                // тестов (все запросы из TestServer считаются с одного IP).
                // Поднимаем до 1000, чтобы реальная rate-limit-логика
                // тестировалась только в targeted-тесте, а не как
                // случайный flake в любом другом.
                ["RateLimiting:Auth:PermitLimit"] = "100000",
                ["RateLimiting:Auth:WindowMinutes"] = "60",

                // Seed:SuperAdmin не задаём — DbInitializer пропустит
                // создание (см. SeedSuperAdminAsync, branch с LogWarning).
            };

            config.AddInMemoryCollection(inMemory);
        });

        // После Build приложения, но ДО первого запроса в тестах —
        // применяем EF миграции к свежему контейнеру Postgres.
        // Без этого таблиц физически нет, и любой Login упадёт с
        // 'relation "users" does not exist'.
        builder.ConfigureServices(_ => { });
    }

    /// <summary>
    /// Поднимает контейнеры и применяет миграции. Вызывается xUnit
    /// до первого теста в коллекции.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _minio.StartAsync();

        // КРИТИЧНО: env-vars выставляем после старта контейнеров, но
        // ДО первого обращения к Services. WebApplicationBuilder
        // в Program.cs читает их синхронно через стандартный
        // EnvironmentVariablesConfigurationSource — на CI нет
        // appsettings.Development.json (он в .gitignore), и без этих
        // env-vars AddInfrastructure fail-fast'нет на пустой connection
        // string ещё до того, как ConfigureAppConfiguration подменит
        // источник. ConfigureAppConfiguration ниже — страховка для
        // IOptions, env-vars — для snapshot-чтения при старте.
        var minioEndpoint = new Uri(_minio.GetConnectionString()).Authority;

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Minio__Endpoint", minioEndpoint);
        Environment.SetEnvironmentVariable("Minio__AccessKey", "minioadmin");
        Environment.SetEnvironmentVariable("Minio__SecretKey", "minioadmin");
        Environment.SetEnvironmentVariable("Minio__UseSsl", "false");
        Environment.SetEnvironmentVariable("Minio__Buckets__DeceasedPhotos", "deceased-photos");
        Environment.SetEnvironmentVariable("Minio__Buckets__GravePhotos", "grave-photos");
        Environment.SetEnvironmentVariable("Minio__Buckets__DeceasedDocuments", "deceased-documents");
        Environment.SetEnvironmentVariable("Minio__Cleanup__Enabled", "false");
        Environment.SetEnvironmentVariable("RefreshTokensCleanup__Enabled", "false");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:5173");

        // КРИТИЧНО: миграции накатываем ДО первого обращения к Services.
        // Program.cs:66 вызывает EnsureMigrationsAppliedAsync синхронно
        // в bootstrap'е приложения и fail-fast'ит при pending migrations.
        // Если триггерить Build через Services без предварительного
        // MigrateAsync — bootstrap упадёт раньше, чем тесты успеют
        // подняться. Поэтому создаём отдельный DbContext в обход хоста,
        // прокатываем миграции, и только потом обращаемся к Services.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using (var migrateContext = new AppDbContext(options))
        {
            await migrateContext.Database.MigrateAsync();
        }

        // Триггерим Build приложения — миграции уже накатаны, поэтому
        // EnsureMigrationsAppliedAsync пройдёт.
        _ = Services;
    }

    /// <summary>
    /// Останавливает контейнеры. Вызывается xUnit после последнего
    /// теста в коллекции.
    /// </summary>
    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _minio.DisposeAsync();
        await base.DisposeAsync();
    }
}
