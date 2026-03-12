using GdeOni.Infrastructure.Persistence;
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
        var provider = configuration["DatabaseProvider"];

        if (string.IsNullOrWhiteSpace(provider))
            throw new InvalidOperationException(
                "Параметр 'DatabaseProvider' не найден. Укажи 'MySql' или 'Postgre'");

        var connectionStringName = provider switch
        {
            "MySql" => "DefaultConnectionMySql",
            "Postgre" => "DefaultConnectionPostgre",
            _ => throw new InvalidOperationException(
                $"Неизвестный провайдер БД: '{provider}'. Допустимые значения: 'MySql', 'Postgre'")
        };

        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' не найдена.");

        services.AddDbContextPool<AppDbContext>(options =>
        {
            switch (provider)
            {
                case "MySql":
                    options.UseMySql(
                        connectionString,
                        ServerVersion.AutoDetect(connectionString));
                    break;

                case "Postgre":
                    options.UseNpgsql(connectionString);
                    break;
            }
            
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

        return services;
    }
}