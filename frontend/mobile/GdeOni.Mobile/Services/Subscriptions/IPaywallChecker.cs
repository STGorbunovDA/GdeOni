namespace GdeOni.Mobile.Services.Subscriptions;

/// <summary>
/// E22.6. Решает, заворачивать ли юзера на paywall после успешного
/// логина — дёргает <c>/api/app/features</c>, <c>/me</c>,
/// <c>/me/subscription</c> и применяет <see cref="Shared.Subscriptions.PaywallEvaluator"/>.
/// </summary>
public interface IPaywallChecker
{
    /// <summary>
    /// true если нужно показать paywall. На сетевой ошибке fail-open
    /// (false) — не блокируем юзера из-за упавшего бэка.
    /// </summary>
    Task<bool> ShouldShowPaywallAsync(CancellationToken cancellationToken = default);
}
