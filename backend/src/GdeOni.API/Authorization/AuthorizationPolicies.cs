namespace GdeOni.API.Authorization;

/// <summary>
/// D16. Константы имён политик авторизации.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Default-политика: требует аутентификации И активной подписки
    /// (Trial / Active / Cancelled-paid-period / Admin-роль).
    /// Применяется ко всем <c>[Authorize]</c> без указания policy.
    /// </summary>
    public const string RequireActiveSubscription = "RequireActiveSubscription";

    /// <summary>
    /// Whitelist-политика: только аутентификация, без проверки
    /// подписки. Используется для эндпоинтов, до которых юзер
    /// должен дойти без оплаты: /me/*, subscription, app/features,
    /// legal.
    /// </summary>
    public const string BasicAuthenticated = "BasicAuthenticated";
}
