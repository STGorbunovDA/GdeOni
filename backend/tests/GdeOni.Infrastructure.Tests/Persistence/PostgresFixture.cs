using DotNet.Testcontainers.Builders;
using GdeOni.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GdeOni.Infrastructure.Tests.Persistence;

/// <summary>
/// Поднимает реальный Postgres-контейнер на всю коллекцию репозиторных
/// тестов. Один контейнер на все классы — иначе каждый класс с
/// IClassFixture тратил бы 5–10 секунд на старт.
///
/// CreateDbContext открывает свежий <see cref="AppDbContext"/> на каждый
/// вызов с применённой миграцией. Тестам не нужно делиться состоянием —
/// каждый тест создаёт собственные сущности с уникальными Guid и Email,
/// поэтому коллизий нет.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("gdeoni_infra_tests")
        .WithUsername("gdeoni")
        .WithPassword("gdeoni_test_pwd")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Прогоняем миграции один раз на старте — чтобы CreateDbContext
        // в тесте не тратил время на проверку схемы.
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
