using GdeOni.Domain.Aggregates.Auth;
using GdeOni.Infrastructure.Persistence;
using GdeOni.Infrastructure.Persistence.Cleanup;
using GdeOni.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Tests.Persistence;

/// <summary>
/// Тесты <see cref="RefreshTokenRepository"/> + RefreshTokensCleanupService.
/// Проверяем round-trip Add/GetByHash, RevokeAllForUser ставит RevokedAtUtc
/// одним SQL UPDATE'ом, и фоновый cleanup удаляет старые токены.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class RefreshTokenRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public RefreshTokenRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Add + GetByHash round-trip: вставляем, читаем по hash, поля совпадают.
    /// </summary>
    [Fact]
    public async Task AddAndGetByHash_RoundTrip()
    {
        var hash = $"hash-{Guid.NewGuid():N}";
        Guid userId;

        await using (var seedContext = _fixture.CreateDbContext())
        {
            var user = TestData.SeedUser(seedContext);
            await seedContext.SaveChangesAsync();
            userId = user.Id;

            var token = RefreshToken.Issue(
                userId,
                hash,
                DateTime.UtcNow.AddDays(7),
                DateTime.UtcNow,
                "127.0.0.1").Value;

            var seedRepo = new RefreshTokenRepository(seedContext);
            await seedRepo.Add(token, CancellationToken.None);
            await seedRepo.Save(CancellationToken.None);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new RefreshTokenRepository(dbContext);

        var loaded = await repo.GetByHash(hash, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.UserId.Should().Be(userId);
        loaded.TokenHash.Should().Be(hash);
        loaded.IsRevoked.Should().BeFalse();
    }

    /// <summary>
    /// RevokeAllForUser: один ExecuteUpdate ставит RevokedAtUtc на все
    /// активные токены пользователя. Уже отозванные не трогает.
    /// </summary>
    [Fact]
    public async Task RevokeAllForUser_SetsRevokedAtUtcOnActiveOnly()
    {
        Guid userId;
        var nowUtc = DateTime.UtcNow;

        await using (var seedContext = _fixture.CreateDbContext())
        {
            var user = TestData.SeedUser(seedContext);
            await seedContext.SaveChangesAsync();
            userId = user.Id;

            var t1 = RefreshToken.Issue(userId, $"h1-{Guid.NewGuid():N}", nowUtc.AddDays(7), nowUtc).Value;
            var t2 = RefreshToken.Issue(userId, $"h2-{Guid.NewGuid():N}", nowUtc.AddDays(7), nowUtc).Value;
            var t3 = RefreshToken.Issue(userId, $"h3-{Guid.NewGuid():N}", nowUtc.AddDays(7), nowUtc).Value;
            t3.Revoke(nowUtc.AddDays(-1));
            seedContext.RefreshTokens.AddRange(t1, t2, t3);
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new RefreshTokenRepository(dbContext);

        await repo.RevokeAllForUser(userId, CancellationToken.None);

        var tokens = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync();

        tokens.Should().HaveCount(3);
        tokens.Where(t => t.TokenHash.StartsWith("h1-") || t.TokenHash.StartsWith("h2-"))
            .Should().OnlyContain(t => t.RevokedAtUtc != null);
        var pre = tokens.Single(t => t.TokenHash.StartsWith("h3-"));
        pre.RevokedAtUtc.Should().NotBeNull();
        pre.RevokedAtUtc!.Value.Should().BeBefore(nowUtc);
    }

    /// <summary>
    /// RefreshTokensCleanupService удаляет revoked > retention и expired > retention.
    /// Запускаем RunOnceAsync через рефлексию, чтобы не ждать таймера BackgroundService.
    /// </summary>
    [Fact]
    public async Task CleanupService_DeletesOldRevokedAndExpired()
    {
        Guid userId;
        var nowUtc = DateTime.UtcNow;

        await using (var seedContext = _fixture.CreateDbContext())
        {
            var user = TestData.SeedUser(seedContext);
            await seedContext.SaveChangesAsync();
            userId = user.Id;

            // Старый revoked (> 30 дней назад) — должен удалиться.
            var oldRevoked = RefreshToken.Issue(
                userId, $"h-old-rev-{Guid.NewGuid():N}",
                nowUtc.AddDays(7), nowUtc.AddDays(-100)).Value;
            oldRevoked.Revoke(nowUtc.AddDays(-100));

            // Старый expired (давно протухший) — тоже удаляется. Создаём с
            // валидным ExpiresAtUtc (Issue требует > now), потом ломаем
            // через рефлексию — иначе Issue вернёт failure.
            var oldExpired = RefreshToken.Issue(
                userId, $"h-old-exp-{Guid.NewGuid():N}",
                nowUtc.AddDays(1), nowUtc).Value;
            typeof(RefreshToken).GetProperty(nameof(RefreshToken.ExpiresAtUtc))!
                .SetValue(oldExpired, nowUtc.AddDays(-100));

            // Свежий активный — остаётся.
            var fresh = RefreshToken.Issue(
                userId, $"h-fresh-{Guid.NewGuid():N}",
                nowUtc.AddDays(7), nowUtc).Value;

            seedContext.RefreshTokens.AddRange(oldRevoked, oldExpired, fresh);
            await seedContext.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o
            .UseNpgsql(_fixture.ConnectionString)
            .UseSnakeCaseNamingConvention());
        services.Configure<RefreshTokensCleanupOptions>(o =>
        {
            o.Enabled = true;
            o.RevokedRetentionDays = 30;
            o.ExpiredRetentionDays = 30;
            o.InitialDelayMinutes = 0;
            o.IntervalHours = 24;
        });
        await using var sp = services.BuildServiceProvider();

        var service = new RefreshTokensCleanupService(
            sp,
            sp.GetRequiredService<IOptions<RefreshTokensCleanupOptions>>(),
            new NullLogger<RefreshTokensCleanupService>());

        var runOnce = typeof(RefreshTokensCleanupService).GetMethod(
            "RunOnceAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)runOnce.Invoke(service, new object[] { CancellationToken.None })!;

        await using var assertContext = _fixture.CreateDbContext();
        var remaining = await assertContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync();

        remaining.Should().HaveCount(1);
        remaining.Single().TokenHash.Should().StartWith("h-fresh-");
    }
}
