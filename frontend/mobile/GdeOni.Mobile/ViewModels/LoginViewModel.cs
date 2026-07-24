using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Services.Notifications;
using GdeOni.Mobile.Services.Subscriptions;
using GdeOni.Mobile.Services.Theming;

namespace GdeOni.Mobile.ViewModels;

public partial class LoginViewModel(
    IAuthService authService,
    IPaywallChecker paywallChecker,
    AnniversariesSyncService anniversariesSync,
    IThemeService themeService) : ObservableObject
{
    // E27. Быстрый переключатель темы на экране входа (зеркало web-кнопки
    // солнце/луна рядом с «ГдеОни»). Показываем иконку ЦЕЛЕВОГО действия:
    // при тёмной теме — солнце (нажми → светлая), при светлой — луну.
    [ObservableProperty]
    private string _themeIcon = themeService.IsDarkTheme ? "☀️" : "🌙";

    [RelayCommand]
    private void ToggleTheme()
    {
        themeService.ToggleLightDark();
        ThemeIcon = themeService.IsDarkTheme ? "☀️" : "🌙";
    }

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

    /// <summary>
    /// D43. Переход к восстановлению пароля. Ссылка нужна именно здесь:
    /// человек понимает, что пароль не подходит, ровно на этом экране.
    /// </summary>
    [RelayCommand]
    private static async Task GoToForgotPasswordAsync()
    {
        await Shell.Current.GoToAsync("forgot-password");
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        await Shell.Current.GoToAsync("register");
    }
}
