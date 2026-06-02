using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Services.Notifications;
using GdeOni.Mobile.Services.Subscriptions;

namespace GdeOni.Mobile.ViewModels;

public partial class LoginViewModel(
    IAuthService authService,
    IPaywallChecker paywallChecker,
    AnniversariesSyncService anniversariesSync) : ObservableObject
{
    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите email и пароль.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await authService.LoginAsync(Email.Trim(), Password);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Не удалось войти.";
                return;
            }

            // Успех — определяем, куда дальше: paywall или main.
            Password = "";

            // E23 C.1. Восстанавливаем annivers-alarms на текущем устройстве:
            // покрывает переустановку APK / смену устройства / то что юзер
            // уже отказал в permissions ранее и теперь готов принять.
            // Запускаем в фоне — не блокируем UI логина.
            _ = Task.Run(() => anniversariesSync.SyncAsync());

            // F17.9 mobile. Подтянуть роль ДО навигации — чтобы вкладка
            // "Админка" появилась сразу при попадании на TabBar.
            if (Shell.Current is AppShell appShell)
                await appShell.RefreshCurrentUserRoleAsync();

            var target = await paywallChecker.ShouldShowPaywallAsync()
                ? "//subscription-required"
                : "//main/tracked";
            await Shell.Current.GoToAsync(target);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        await Shell.Current.GoToAsync("register");
    }
}
