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
        // Заодно выбиваем кеш subscription-access (см. ActiveSubscription
        // AuthorizationHandler). Любая операция, ротирующая SecurityStamp
        // (Block/Unblock, ChangeRole, RevokeSubscription, Complimentary
        // grant/revoke), потенциально меняет и статус доступа — должны
        // их инвалидировать вместе, чтобы избежать рассинхрона.
        memoryCache.Remove(DependencyInjection.SubscriptionAccessCacheKey(userId));
    }
}
