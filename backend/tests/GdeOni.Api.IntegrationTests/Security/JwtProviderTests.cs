using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GdeOni.API.Security;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Aggregates.User;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GdeOni.Api.IntegrationTests.Security;

/// <summary>
/// Тесты <see cref="JwtProvider"/> — генератор access-токенов.
/// JWT — это отформатированная строка, поэтому без БД и без сети
/// можно проверить claims и expiration через JwtSecurityTokenHandler.
/// SecurityStamp кеш — IMemoryCache, тоже in-memory.
/// </summary>
public sealed class JwtProviderTests
{
    /// <summary>
    /// GenerateAccessToken кладёт NameIdentifier / Email / Name / Role /
    /// SecurityStamp / Jti claims. Это контракт, на котором держится
    /// CurrentUserService и роль-based авторизация.
    /// </summary>
    [Fact]
    public void GenerateAccessToken_PutsExpectedClaims()
    {
        var (provider, _, options) = BuildProvider();
        var user = User.Register("user@example.com", "hash", userName: "alice").Value;

        var token = provider.GenerateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        jwt.Issuer.Should().Be(options.Issuer);
        jwt.Audiences.Should().Contain(options.Audience);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Name && c.Value == user.UserName);
        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == user.Role.ToString());
        jwt.Claims.Should().Contain(c =>
            c.Type == JwtClaimNames.SecurityStamp && c.Value == user.SecurityStamp.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    /// <summary>
    /// Expiration = now + AccessTokenLifetimeMinutes. Допуск ±2 секунды
    /// на разницу между моментом генерации и assertion'ом.
    /// </summary>
    [Fact]
    public void GenerateAccessToken_ExpirationMatchesLifetime()
    {
        var (provider, _, options) = BuildProvider();
        var user = User.Register("exp@example.com", "hash").Value;

        var nowBefore = DateTime.UtcNow;
        var token = provider.GenerateAccessToken(user);
        var nowAfter = DateTime.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        jwt.ValidTo.Should().BeOnOrAfter(nowBefore.AddMinutes(options.AccessTokenLifetimeMinutes).AddSeconds(-2));
        jwt.ValidTo.Should().BeOnOrBefore(nowAfter.AddMinutes(options.AccessTokenLifetimeMinutes).AddSeconds(2));

        token.ExpiresAtUtc.Should().BeOnOrAfter(nowBefore.AddMinutes(options.AccessTokenLifetimeMinutes).AddSeconds(-2));
        token.ExpiresAtUtc.Should().BeOnOrBefore(nowAfter.AddMinutes(options.AccessTokenLifetimeMinutes).AddSeconds(2));
    }

    /// <summary>
    /// После GenerateAccessToken SecurityStamp пользователя лежит
    /// в IMemoryCache. Это write-through из <see cref="JwtProvider"/>:
    /// после смены пароля/роли новый токен сразу действителен, а кеш
    /// не отдаёт старый stamp.
    /// </summary>
    [Fact]
    public void GenerateAccessToken_WritesSecurityStampToCache()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var (provider, _, _) = BuildProvider(cache);
        var user = User.Register("cache@example.com", "hash").Value;

        provider.GenerateAccessToken(user);

        // Ключ — internal в API.DependencyInjection. Для теста реконструируем
        // тот же формат: "secstamp:{userId}" (см. SecurityStampCacheKey).
        var cacheKey = $"secstamp:{user.Id}";
        cache.TryGetValue<Guid?>(cacheKey, out var cachedStamp).Should().BeTrue();
        cachedStamp.Should().Be(user.SecurityStamp);
    }

    private static (JwtProvider Provider, IMemoryCache Cache, JwtOptions Options) BuildProvider(
        IMemoryCache? cache = null)
    {
        var options = new JwtOptions
        {
            Issuer = "GdeOni.Tests",
            Audience = "GdeOni.Tests.Client",
            SecretKey = "test-secret-key-with-at-least-32-bytes!!",
            AccessTokenLifetimeMinutes = 30,
            SecurityStampCacheTtlSeconds = 30
        };
        cache ??= new MemoryCache(new MemoryCacheOptions());
        var provider = new JwtProvider(Options.Create(options), cache);
        return (provider, cache, options);
    }
}
