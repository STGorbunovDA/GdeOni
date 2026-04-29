using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Security;
using GdeOni.Infrastructure.Data;
using GdeOni.Infrastructure.Persistence;
using GdeOni.Infrastructure.Persistence.Repositories;
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

        services.AddDbContextPool<AppDbContext>(options =>
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
        });

        services.AddScoped<IDeceasedRepository, DeceasedRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IRefreshTokenFactory, RefreshTokenFactory>();

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));

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