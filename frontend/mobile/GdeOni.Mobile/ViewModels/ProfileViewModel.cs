using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Auth;

namespace GdeOni.Mobile.ViewModels;

public partial class ProfileViewModel(IAuthService authService) : ObservableObject
{
    [ObservableProperty]
    private string _title = "Профиль";

    [ObservableProperty]
    private string? _userName;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _fullName;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// E22. Версия приложения для отображения внизу профиля. Особенно
    /// важна при sideload-распространении (D17.2) — без автообновления
    /// пользователь и саппорт должны видеть, какая версия установлена.
    /// </summary>
    public string AppVersion { get; } =
        $"Версия {AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})";

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var me = await authService.GetCurrentUserAsync();
            if (me is null)
            {
                ErrorMessage = "Не удалось получить данные профиля.";
                return;
            }

            UserName = me.UserName;
            Email = me.Email;
            FullName = me.FullName;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        await Shell.Current.GoToAsync("change-password");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            IsBusy = true;
            await authService.LogoutAsync();

            // Очищаем UI-state перед уходом, чтобы при возврате не светились
            // данные предыдущего пользователя.
            UserName = null;
            Email = null;
            FullName = null;
            ErrorMessage = null;

            await Shell.Current.GoToAsync("//login");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
