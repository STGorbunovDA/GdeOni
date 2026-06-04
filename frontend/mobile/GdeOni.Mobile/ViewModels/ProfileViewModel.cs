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
    [NotifyPropertyChangedFor(nameof(ShowSubscriptionBlock))]
    [NotifyPropertyChangedFor(nameof(IsAdmin))]
    [NotifyPropertyChangedFor(nameof(IsStaff))]
    private string? _role;

    /// <summary>
    /// SuperAdmin / Admin — доступ к админ-странице (управление юзерами,
    /// платежами, complimentary). Manager НЕ входит — он сотрудник без
    /// прав модерации других юзеров.
    /// </summary>
    public bool IsAdmin =>
        string.Equals(Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Staff-роли (SuperAdmin/Admin/Manager) не платят подписку — бэк
    /// пускает их без неё (см. D16.5), поэтому блок "Подписка" в Profile
    /// им не показываем.
    /// </summary>
    public bool IsStaff =>
        IsAdmin
        || string.Equals(Role, "Manager", StringComparison.OrdinalIgnoreCase);

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    // ───────── E22. Subscription block ─────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubscriptionSummary))]
    [NotifyPropertyChangedFor(nameof(SubscriptionDetail))]
    [NotifyPropertyChangedFor(nameof(HasSubscriptionData))]
    [NotifyPropertyChangedFor(nameof(ShowSubscriptionBlock))]
    private MySubscriptionResponse? _subscription;

    public bool HasSubscriptionData => Subscription is not null;

    /// <summary>
    /// Показываем блок только обычным юзерам — staff (Admin/SuperAdmin/Manager)
    /// подписка не нужна (бэк их освобождает в гейте, см. D16.5).
    /// </summary>
    public bool ShowSubscriptionBlock => HasSubscriptionData && !IsStaff;

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
                "PendingPayment" => "Ожидаем подтверждения оплаты",
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
                "Trial" or "Active" or "Cancelled" or "PendingPayment" when Subscription.ExpiresAtUtc is { } expiry =>
                    $"До {expiry.ToLocalTime():dd.MM.yyyy} ({Subscription.DaysUntilExpiry} дн.)",
                "Expired" => "Срок подписки закончился. Оформите подписку для продолжения.",
                _ => "Оформите подписку для доступа к сервису.",
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
            Role = me.Role;

            // Staff-юзерам (SuperAdmin/Admin/Manager) блок подписки в UI скрыт —
            // нет смысла дёргать /me/subscription (и тратить запрос, и
            // нагружать бэк).
            if (!IsStaff)
            {
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
            else
            {
                Subscription = null;
            }
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

    /// <summary>
    /// F17.9 mobile. Открыть админ-страницу. Кнопка видна только при
    /// IsAdmin == true; backend сам отдаст 403 если что-то рассинхронится.
    /// </summary>
    [RelayCommand]
    private async Task OpenAdminAsync()
    {
        await Shell.Current.GoToAsync("admin");
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
            Role = null;

            await Shell.Current.GoToAsync("//login");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
