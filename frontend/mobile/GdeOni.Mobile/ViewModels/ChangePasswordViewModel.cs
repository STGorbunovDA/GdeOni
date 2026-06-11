using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Shared.Auth;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// E18. Смена пароля в профиле. После успешного PUT backend ротирует
/// SecurityStamp — текущий access-токен инвалидируется. Поэтому после
/// 200 OK сразу LogoutAsync() (best-effort), очищаем токены и редирект
/// на login: при следующем входе юзер получит свежие токены с новым
/// SecurityStamp в claim'е.
/// </summary>
public partial class ChangePasswordViewModel(
    IUsersApi usersApi,
    IAuthService authService) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private string _currentPassword = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyPropertyChangedFor(nameof(IsNewPasswordTooShort))]
    [NotifyPropertyChangedFor(nameof(IsNewPasswordTooLong))]
    [NotifyPropertyChangedFor(nameof(PasswordsMatch))]
    private string _newPassword = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyPropertyChangedFor(nameof(PasswordsMatch))]
    private string _confirmPassword = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsNewPasswordTooShort => PasswordRules.IsTooShort(NewPassword);
    public bool IsNewPasswordTooLong => PasswordRules.IsTooLong(NewPassword);
    public bool PasswordsMatch => PasswordRules.PasswordsMatch(NewPassword, ConfirmPassword);
    public bool CanSubmit => PasswordRules.CanSubmit(CurrentPassword, NewPassword, ConfirmPassword);

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var me = await authService.GetCurrentUserAsync();
            if (me is null)
            {
                ErrorMessage = "Не удалось получить идентификатор пользователя. Попробуйте перезайти.";
                return;
            }

            var envelope = await usersApi.ChangePasswordAsync(
                me.Id,
                new ChangePasswordRequest(CurrentPassword, NewPassword));

            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось сменить пароль.";
                return;
            }

            var page = Shell.Current?.CurrentPage;
            if (page is not null)
            {
                await page.DisplayAlertAsync(
                    "Пароль изменён",
                    "Войдите снова с новым паролем.",
                    "OK");
            }

            // SecurityStamp ротирован — токены недействительны. Чистим
            // SecureStorage и кидаем на login.
            await authService.LogoutAsync();
            await Shell.Current!.GoToAsync("//login");
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (HttpRequestException httpEx)
        {
            ErrorMessage = $"Сетевая ошибка: {httpEx.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
