using GdeOni.Application.Common.Security;
using Microsoft.Extensions.Caching.Memory;

namespace GdeOni.API.Security;

/// <summary>
/// Реализация ISecurityStampInvalidator поверх IMemoryCache, который
/// использует JwtBearerEvents.OnTokenValidated для read-through
/// проверки security_stamp. Выкидываем запись по ключу — следующий
/// запрос с access-токеном попадёт в SELECT по БД, увидит новый
/// stamp и завалит токен (D11.8.1).
/// </summary>
public sealed class SecurityStampInvalidator(IMemoryCache memoryCache) : ISecurityStampInvalidator
{
    public void Invalidate(Guid userId)
    {
        memoryCache.Remove(DependencyInjection.SecurityStampCacheKey(userId));
    }
}
