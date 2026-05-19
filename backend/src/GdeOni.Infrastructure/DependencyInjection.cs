using GdeOni.Application.Abstractions.Features;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Routing;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Security;
using GdeOni.Infrastructure.Data;
using GdeOni.Infrastructure.Features;
using GdeOni.Infrastructure.Persistence;
using GdeOni.Infrastructure.Persistence.Cleanup;
using GdeOni.Infrastructure.Persistence.Repositories;
using GdeOni.Infrastructure.Routing;
using GdeOni.Infrastructure.Security;
using GdeOni.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;

namespace GdeOni.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' не найдена.");

        services.AddDbContextPool<AppDbContext>(
            optionsAction: options =>
            {
                options.UseNpgsql(connectionString);
                options.UseSnakeCaseNamingConvention();

                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.LogTo(
                        Console.WriteLine,
                        new[] { DbLoggerCategory.Database.Command.Name },
                        LogLevel.Information);
                }
            },
            // Явный poolSize вместо EF-дефолта 1024 (D11.7.5). 128 хватит
            // для сценария "один кэш-инстанс на одновременный запрос";
            // если выше — можно поднять через перегрузку без изменения
            // кода вызывающих repos.
            poolSize: 128);

        services.AddScoped<IDeceasedRepository, DeceasedRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        // PasswordHasher без состояния: BCrypt.Net не использует поля
        // экземпляра. Singleton экономит аллокацию на каждый запрос
        // (см. D11.7.2).
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IRefreshTokenFactory, RefreshTokenFactory>();

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        services.Configure<BCryptOptions>(configuration.GetSection(BCryptOptions.SectionName));

        // D17. Feature flags читаются через IOptionsMonitor для
        // hot-reload без рестарта. Сервис без состояния → Singleton.
        services.Configure<FeatureFlagsOptions>(
            configuration.GetSection(FeatureFlagsOptions.SectionName));
        services.AddSingleton<IFeatureFlagService, OptionsFeatureFlagService>();

        services.AddSingleton<IMinioClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MinioOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.Endpoint))
                throw new InvalidOperationException("MinIO Endpoint не сконфигурирован.");
            if (string.IsNullOrWhiteSpace(options.AccessKey))
                throw new InvalidOperationException("MinIO AccessKey не сконфигурирован.");
            if (string.IsNullOrWhiteSpace(options.SecretKey))
                throw new InvalidOperationException("MinIO SecretKey не сконфигурирован.");

            var builder = new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey);

            if (options.UseSsl)
                builder = builder.WithSSL();

            return builder.Build();
        });

        services.AddSingleton<IFileStorage, MinioFileStorage>();

        // D8.2: deep-link провайдеры карт. Singleton — без состояния,
        // только форматируют URL без сетевых вызовов.
        services.AddSingleton<IRouteLinkProvider, YandexRouteLinkProvider>();
        services.AddSingleton<IRouteLinkProvider, GoogleMapsRouteLinkProvider>();
        services.AddSingleton<IRouteLinkProvider, TwoGisRouteLinkProvider>();

        services.AddHostedService<MinioOrphanCleanupService>();

        services.Configure<RefreshTokensCleanupOptions>(
            configuration.GetSection(RefreshTokensCleanupOptions.SectionName));
        services.AddHostedService<RefreshTokensCleanupService>();

        return services;
    }

    public static Task SeedDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        return DbInitializer.SeedSuperAdminAsync(services, cancellationToken);
    }

    public static Task BootstrapStorageAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        return MinioBootstrap.EnsureBucketsAsync(services, cancellationToken);
    }
}