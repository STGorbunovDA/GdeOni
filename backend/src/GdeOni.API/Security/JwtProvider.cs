using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Aggregates.User;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GdeOni.API.Security;

/// <summary>
/// Реализация <see cref="IJwtProvider"/> на базе
/// <see cref="JwtSecurityTokenHandler"/>. Формирует подписанный
/// HS256 access-токен с минимальным набором claim'ов (без PII).
/// </summary>
public sealed class JwtProvider(
    IOptions<JwtOptions> options,
    IMemoryCache memoryCache,
    TimeProvider timeProvider)
    : IJwtProvider
{
    private readonly JwtOptions _options = options.Value;

    /// <summary>
    /// Генерирует подписанный JWT для пользователя и одновременно
    /// прогревает кеш SecurityStamp (write-through).
    /// </summary>
    public AccessToken GenerateAccessToken(User user)
    {
        // PII в JWT — это утечка: токен живёт в local storage клиента
        // и читается через base64 без расшифровки. Email и UserName
        // никем не используются (CurrentUserService берёт только id+role),
        // их можно получить отдельным /users/me. 152-ФЗ + принцип
        // минимизации PII требуют не таскать лишнее в долгоживущих
        // артефактах.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtClaimNames.SecurityStamp, user.SecurityStamp.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var expiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        var serialized = new JwtSecurityTokenHandler().WriteToken(token);

        // Write-through: после генерации нового токена кладём актуальный
        // SecurityStamp в кеш. Иначе если в кеше остался старый stamp от
        // предыдущей сессии, новый только что выпущенный токен схлопочет
        // 401 на первом же запросе до истечения TTL.
        memoryCache.Set(
            DependencyInjection.SecurityStampCacheKey(user.Id),
            (Guid?)user.SecurityStamp,
            TimeSpan.FromSeconds(_options.SecurityStampCacheTtlSeconds));

        return new AccessToken(serialized, expiresAtUtc);
    }
}
