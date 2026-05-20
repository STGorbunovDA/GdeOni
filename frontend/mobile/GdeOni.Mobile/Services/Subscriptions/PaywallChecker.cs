using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Shared.Subscriptions;
using Refit;

namespace GdeOni.Mobile.Services.Subscriptions;

public sealed class PaywallChecker(
    IAppApi appApi,
    ISubscriptionsApi subscriptionsApi,
    IAuthService authService) : IPaywallChecker
{
    public async Task<bool> ShouldShowPaywallAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1) Features — если SubscriptionEnabled=false, дальше можно
            //    не ходить (open-beta).
            var featuresEnv = await appApi.GetFeaturesAsync(cancellationToken);
            var features = featuresEnv.Result;
            if (features is null)
                return false;
            if (!features.SubscriptionEnabled)
                return false;

            // 2) Роль — admin'ы освобождены от подписки.
            var me = await authService.GetCurrentUserAsync(cancellationToken);
            var role = me?.Role;

            // 3) Текущий статус подписки.
            var subEnv = await subscriptionsApi.GetMyAsync(cancellationToken);
            var isActiveNow = subEnv.Result?.IsActiveNow ?? false;

            return PaywallEvaluator.ShouldShowPaywall(
                subscriptionEnabled: features.SubscriptionEnabled,
                userRole: role,
                isActiveNow: isActiveNow);
        }
        catch (ApiException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}
