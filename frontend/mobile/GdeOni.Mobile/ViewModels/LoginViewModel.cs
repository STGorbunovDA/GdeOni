using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Services.Subscriptions;

namespace GdeOni.Mobile.ViewModels;

public partial class LoginViewModel(
    IAuthService authService,
    IPaywallChecker paywallChecker) : ObservableObject
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
