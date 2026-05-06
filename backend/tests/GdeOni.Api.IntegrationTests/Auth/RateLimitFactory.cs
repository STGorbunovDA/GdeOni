using DotNet.Testcontainers.Builders;
using GdeOni.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;

namespace GdeOni.Api.IntegrationTests.Auth;

/// <summary>
/// Отдельная фабрика для rate-limit теста с PermitLimit=3 (вместо
/// 100000 в общей фабрике). RateLimitingExtensions читает PermitLimit
/// snapshot'ом при старте, поэтому нужна отдельная инстанция Web-app
/// с другими env-vars и in-memory config'ом.
/// </summary>
public sealed class RateLimitedWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // RateLimit-snapshot в AddCustomRateLimiting читает IConfiguration
    // в момент сборки приложения. In-memory из ConfigureAppConfiguration
    // на практике не подхватывается snapshot-pattern'ом — поэтому
    // ставим env-vars (двойное подчёркивание = nested key).
    //
    // Process-wide env-vars пересекаются с GdeOniWebAppFactory.
    // Чтобы избежать гонок, общая Integration-коллекция и
    // RateLimitTests (default-collection) сериализуются через
    // xunit.runner.json: parallelizeTestCollections=false.
    static RateLimitedWebAppFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Issuer", "GdeOni.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "GdeOni.Tests.Client");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "test-secret-key-with-at-least-32-bytes!!");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenLifetimeMinutes", "30");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenLifetimeDays", "7");
        Environment.SetEnvironmentVariable("Jwt__SecurityStampCacheTtlSeconds", "30");
    }

    /// <summary>
    /// Перед стартом устанавливаем PermitLimit=3 в env-vars (snapshot
    /// в AddCustomRateLimiting). После теста — возвращаем 100000,
    /// чтобы Integration-коллекция, если запустится после, не упёрлась
    /// в rate-limit.
    /// </summary>
    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("RateLimiting__Auth__PermitLimit", "3");
        Environment.SetEnvironmentVariable("RateLimiting__Auth__WindowMinutes", "60");

        await _postgres.StartAsync();
        await _minio.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("RateLimiting__Auth__PermitLimit", "100000");
        Environment.SetEnvironmentVariable("RateLimiting__Auth__WindowMinutes", "60");

        await _postgres.DisposeAsync();
        await _minio.DisposeAsync();
        await base.DisposeAsync();
    }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("gdeoni_rl_test")
        .WithUsername("gdeoni")
        .WithPassword("gdeoni_test_pwd")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
        .Build();

    private readonly MinioContainer _minio = new MinioBuilder()
        .WithImage("minio/minio:latest")
        .WithUsername("minioadmin")
        .WithPassword("minioadmin")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration(config =>
        {
            config.Sources.Clear();

            var minioEndpoint = new Uri(_minio.GetConnectionString()).Authority;

            var inMemory = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = "GdeOni.Tests",
                ["Jwt:Audience"] = "GdeOni.Tests.Client",
                ["Jwt:SecretKey"] = "test-secret-key-with-at-least-32-bytes!!",
                ["Jwt:AccessTokenLifetimeMinutes"] = "30",
                ["Jwt:RefreshTokenLifetimeDays"] = "7",
                ["Jwt:SecurityStampCacheTtlSeconds"] = "30",
                ["Minio:Endpoint"] = minioEndpoint,
                ["Minio:AccessKey"] = "minioadmin",
                ["Minio:SecretKey"] = "minioadmin",
                ["Minio:UseSsl"] = "false",
                ["Minio:Buckets:DeceasedPhotos"] = "deceased-photos",
                ["Minio:Buckets:GravePhotos"] = "grave-photos",
                ["Minio:Buckets:DeceasedDocuments"] = "deceased-documents",
                ["Minio:Cleanup:Enabled"] = "false",
                ["RefreshTokensCleanup:Enabled"] = "false",
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                ["RateLimiting:Auth:PermitLimit"] = "3",
                ["RateLimiting:Auth:WindowMinutes"] = "60"
            };

            config.AddInMemoryCollection(inMemory);
        });
    }

}
