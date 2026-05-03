using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Aggregates.User;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GdeOni.API.Security;

public sealed class JwtProvider(
    IOptions<JwtOptions> options,
    IMemoryCache memoryCache)
    : IJwtProvider
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtClaimNames.SecurityStamp, user.SecurityStamp.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);

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
