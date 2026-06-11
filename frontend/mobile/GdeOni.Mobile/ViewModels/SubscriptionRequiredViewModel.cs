using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Auth;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// E22.6. ViewModel для глобального paywall'а. Юзер попадает сюда:
///   - после Login, если SubscriptionEnabled && !IsActiveNow && !IsAdmin;
///   - в середине сессии, если backend вернул 403 subscription.required
///     (см. SubscriptionGateHandler).
/// Из paywall возможен только переход на SubscriptionPage (оформить)
/// или выход из аккаунта.
/// </summary>
public partial class SubscriptionRequiredViewModel(IAuthService authService) : ObservableObject
{
    [ObservableProperty]
    private string _title = "Нужна подписка";

    [ObservableProperty]
    private string _message =
        "Чтобы пользоваться приложением, оформите подписку — 49 ₽/мес. " +
        "Доступ ко всем функциям без ограничений.";

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task SubscribeAsync()
    {
        await Shell.Current.GoToAsync("subscription");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            IsBusy = true;
            await authService.LogoutAsync();
            await Shell.Current.GoToAsync("//login");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
