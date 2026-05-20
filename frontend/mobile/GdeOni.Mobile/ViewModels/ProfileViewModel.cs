using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Auth;
using Refit;

namespace GdeOni.Mobile.ViewModels;

public partial class ProfileViewModel(
    IAuthService authService,
    ISubscriptionsApi subscriptionsApi) : ObservableObject
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

    // ───────── E22. Subscription block ─────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubscriptionSummary))]
    [NotifyPropertyChangedFor(nameof(SubscriptionDetail))]
    [NotifyPropertyChangedFor(nameof(HasSubscriptionData))]
    private MySubscriptionResponse? _subscription;

    public bool HasSubscriptionData => Subscription is not null;

    /// <summary>
    /// Краткое название состояния — большим текстом в карточке.
    /// </summary>
    public string SubscriptionSummary
    {
        get
        {
            if (Subscription is null) return "Загрузка…";
            if (Subscription.HasComplimentaryAccess)
                return "Бесплатный доступ от администратора";

            return Subscription.Status switch
            {
                "Trial" => "Пробный период",
                "Active" => "Подписка активна",
                "Cancelled" => "Подписка отменена",
                "Expired" => "Подписка истекла",
                _ => "Подписка не оформлена",
            };
        }
    }

    /// <summary>
    /// Подробности — мелким серым: даты, дни, причина (для complimentary).
    /// </summary>
    public string SubscriptionDetail
    {
        get
        {
            if (Subscription is null) return string.Empty;

            if (Subscription.HasComplimentaryAccess)
            {
                if (Subscription.ComplimentaryAccessUntilUtc is { } until)
                {
                    var localUntil = until.ToLocalTime();
                    var note = string.IsNullOrWhiteSpace(Subscription.ComplimentaryAccessNote)
                        ? string.Empty
                        : $"\nПричина: {Subscription.ComplimentaryAccessNote}";
                    return $"До {localUntil:dd.MM.yyyy} ({Subscription.DaysUntilExpiry} дн.)" + note;
                }

                return string.IsNullOrWhiteSpace(Subscription.ComplimentaryAccessNote)
                    ? "Бессрочно"
                    : $"Бессрочно\nПричина: {Subscription.ComplimentaryAccessNote}";
            }

            return Subscription.Status switch
            {
                "Trial" or "Active" or "Cancelled" when Subscription.ExpiresAtUtc is { } expiry =>
                    $"До {expiry.ToLocalTime():dd.MM.yyyy} ({Subscription.DaysUntilExpiry} дн.)",
                "Expired" => "Срок подписки закончился.",
                _ => "Доступ возможен только с активной подпиской.",
            };
        }
    }

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

            // Подписка — best-effort. Если упадёт (нет сети, бэк лёг) —
            // не ломаем весь экран профиля, просто оставляем "Загрузка…".
            try
            {
                var subEnvelope = await subscriptionsApi.GetMyAsync();
                Subscription = subEnvelope.Result;
            }
            catch (ApiException) { }
            catch (HttpRequestException) { }
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
    private async Task ManageSubscriptionAsync()
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

            // Очищаем UI-state перед уходом, чтобы при возврате не светились
            // данные предыдущего пользователя.
            UserName = null;
            Email = null;
            FullName = null;
            Subscription = null;
            ErrorMessage = null;

            await Shell.Current.GoToAsync("//login");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
