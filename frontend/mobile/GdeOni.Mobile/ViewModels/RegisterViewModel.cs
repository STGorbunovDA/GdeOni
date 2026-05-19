using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Auth;

namespace GdeOni.Mobile.ViewModels;

public partial class RegisterViewModel(IAuthService authService) : ObservableObject
{
    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string _userName = "";

    [ObservableProperty]
    private string _fullName = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _passwordConfirm = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email и пароль обязательны.";
            return;
        }

        if (Password != PasswordConfirm)
        {
            ErrorMessage = "Пароли не совпадают.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await authService.RegisterAsync(
                Email.Trim(),
                string.IsNullOrWhiteSpace(UserName) ? null : UserName.Trim(),
                string.IsNullOrWhiteSpace(FullName) ? null : FullName.Trim(),
                Password);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Не удалось зарегистрироваться.";
                return;
            }

            // RegisterAsync уже сделал auto-login и сохранил токены.
            Password = "";
            PasswordConfirm = "";
            await Shell.Current.GoToAsync("//main/tracked");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackToLoginAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}