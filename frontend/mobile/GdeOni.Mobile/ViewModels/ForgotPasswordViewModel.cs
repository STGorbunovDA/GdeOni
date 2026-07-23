using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// D43. Экран «Забыли пароль»: пользователь вводит email, на почту
/// уходит ссылка для смены пароля.
///
/// Сама смена происходит НА САЙТЕ — ссылка из письма ведёт на
/// gdeoni.ru/reset-password. Экрана ввода нового пароля в приложении
/// нет намеренно: единственный способ доставить туда токен — заставить
/// человека переписывать его из письма руками, что хуже во всех
/// отношениях. Ссылка открывается браузером телефона и работает.
///
/// Текст результата обязан быть УСЛОВНЫМ («если аккаунт существует»):
/// бэк намеренно отвечает одинаково для зарегистрированных и нет, чтобы
/// нельзя было перебором выяснить, кто есть в сервисе. Прямое «письмо
/// отправлено» сломало бы эту защиту прямо в интерфейсе.
/// </summary>
public partial class ForgotPasswordViewModel(IAuthApi authApi) : ObservableObject
{
    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    /// <summary>
    /// Показывать ли экран «проверьте почту» вместо формы.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormVisible))]
    private bool _isSent;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsFormVisible => !IsSent;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Введите email.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            await authApi.ForgotPasswordAsync(new ForgotPasswordRequest(Email.Trim()));

            IsSent = true;
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"Не удалось отправить запрос (HTTP {(int)apiEx.StatusCode}).";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Нет связи с сервером. Проверьте интернет.";
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Сервер не ответил вовремя. Попробуйте ещё раз.";
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
    private static async Task BackToLoginAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
