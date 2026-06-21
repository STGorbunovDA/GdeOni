using System.Security.Claims;
using GdeOni.Application.Abstractions.Features;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GdeOni.API.Authorization;

/// <summary>
/// D16. Handler для <see cref="ActiveSubscriptionRequirement"/>.
///
/// <para>Логика (по убыванию приоритета):</para>
/// <list type="number">
///   <item>Если <see cref="IFeatureFlagService.IsSubscriptionEnabled"/>
///   = false — Succeed (коммерциализация выключена, всем доступно).</item>
///   <item>Если в claim'ах роль Admin / SuperAdmin — Succeed (админы
///   освобождены от оплаты, решение 2026-05-14).</item>
///   <item>D22: если у юзера активен complimentary access (granted by
///   admin) — Succeed без проверки подписки.</item>
///   <item>Иначе грузим User по claim'у и проверяем
///   <c>HasActiveSubscription</c> с учётом GracePeriodDaysAfterExpiry.</item>
/// </list>
///
/// <para>Handler живёт в API-слое, т.к. зависит от
/// <see cref="IAuthorizationHandler"/> (ASP.NET Core). Application
/// предоставляет только Repository и FeatureFlag-сервис.</para>
/// </summary>
public sealed class ActiveSubscriptionAuthorizationHandler(
    IFeatureFlagService featureFlags,
    IServiceScopeFactory scopeFactory,
    IMemoryCache memoryCache,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider)
    : AuthorizationHandler<ActiveSubscriptionRequirement>
{
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(
        Math.Max(1, jwtOptions.Value.SecurityStampCacheTtlSeconds));

    /// <summary>
    /// Проверяет, есть ли у текущего пользователя право доступа,
    /// требующее активной подписки (с учётом complimentary access,
    /// админских ролей и feature-flag коммерциализации).
    /// </summary>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveSubscriptionRequirement requirement)
    {
        // 1) Коммерциализация выключена → все proходят.
        if (!featureFlags.IsSubscriptionEnabled)
        {
            context.Succeed(requirement);
            return;
        }

        // 2) Юзер не аутентифицирован → fail (теоретически сюда не
        // дойдёт — DefaultPolicy включает RequireAuthenticatedUser
        // первым, но защита от mis-configured-политики).
        if (context.User.Identity is not { IsAuthenticated: true })
        {
            return;
        }

        // 3) Admin / SuperAdmin / Manager → bypass подписки. Manager
        // получает доступ ко всем функциям бесплатно (как штатные
        // сотрудники-модераторы), но НЕ имеет админ-прав в других
        // местах (CanEditDeceasedPolicy, AdminController-ы остаются
        // только для Admin/SuperAdmin). Проверка по claim'у — у юзера
        // смена роли ротирует SecurityStamp и инвалидирует токен.
        if (context.User.IsInRole(UserRole.SuperAdmin.ToString())
            || context.User.IsInRole(UserRole.Admin.ToString())
            || context.User.IsInRole(UserRole.Manager.ToString()))
        {
            context.Succeed(requirement);
            return;
        }

        // 4) Грузим User и проверяем доменный метод. ScopeFactory —
        // потому что IUserRepository Scoped, а handler Singleton
        // (Singleton зависит от Singleton'ов AuthorizationOptions).
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return;

        // Read-through кеш по userId с TTL = SecurityStampCacheTtlSeconds.
        // Раньше каждый запрос за защищённым ресурсом = SELECT по users
        // (~1000 RPS = 1000 SELECT). Теперь — один SELECT раз в 30 сек,
        // плюс мгновенная инвалидация через SecurityStampInvalidator
        // (Block/RoleChange/RevokeSubscription/Complimentary grant/revoke
        // уже его дёргают).
        var cacheKey = DependencyInjection.SubscriptionAccessCacheKey(userId);
        if (memoryCache.TryGetValue<bool>(cacheKey, out var cachedAccess))
        {
            if (cachedAccess) context.Succeed(requirement);
            return;
        }

        // Берём cancellation token из текущего HttpContext, если он
        // есть. Раньше тут было CancellationToken.None — handler дёргается
        // на каждом авторизованном запросе, и при дисконнекте клиента
        // запрос продолжал работать (graceful shutdown тоже не работал).
        var ct = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;

        await using var scope = scopeFactory.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await userRepo.GetByIdReadOnly(userId, ct);
        if (user is null)
        {
            // Отрицательный результат тоже кешируем — иначе атакующий
            // с украденным токеном удалённого юзера каждый раз заставляет
            // нас идти в БД и получать null.
            memoryCache.Set(cacheKey, false, _cacheTtl);
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hasAccess = user.HasComplimentaryAccess(now)
            || user.HasActiveSubscription(now, featureFlags.GracePeriodDaysAfterExpiry);

        memoryCache.Set(cacheKey, hasAccess, _cacheTtl);
        if (hasAccess)
            context.Succeed(requirement);
    }
}
