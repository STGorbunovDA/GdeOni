using System.Security.Claims;
using GdeOni.Application.Abstractions.Features;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Authorization;

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
    IServiceScopeFactory scopeFactory)
    : AuthorizationHandler<ActiveSubscriptionRequirement>
{
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

        // 3) Admin / SuperAdmin → bypass подписки. Проверка по claim'у,
        // не по hit'у в БД — у админа смена роли всё равно ротирует
        // SecurityStamp и инвалидирует токен.
        if (context.User.IsInRole(UserRole.Admin.ToString())
            || context.User.IsInRole(UserRole.SuperAdmin.ToString()))
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

        await using var scope = scopeFactory.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await userRepo.GetByIdReadOnly(userId, CancellationToken.None);
        if (user is null)
            return;

        if (user.HasActiveSubscription(DateTime.UtcNow, featureFlags.GracePeriodDaysAfterExpiry))
        {
            context.Succeed(requirement);
        }
    }
}
