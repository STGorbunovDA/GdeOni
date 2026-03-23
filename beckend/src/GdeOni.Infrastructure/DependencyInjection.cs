using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Infrastructure.Persistence;
using GdeOni.Infrastructure.Persistence.Repositories;
using GdeOni.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        return services;
    }
}