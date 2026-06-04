using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Auth;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// F17.9 mobile. Детальная карточка юзера: смена роли + выдача/отзыв
/// бесплатного доступа. Подписку и платежи юзера не показываем —
/// для этого есть отдельный раздел "Платежи" в админке.
/// </summary>
[QueryProperty(nameof(UserId), "userId")]
public partial class AdminUserDetailsViewModel(
    IAdminApi adminApi,
    IAuthService authService) : ObservableObject
{
    [ObservableProperty] private string _userId = "";

    partial void OnUserIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = LoadAsync();
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isBusyAction;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string? _statusMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    [ObservableProperty] private string _displayName = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _role = "";
    [ObservableProperty] private string _registered = "";
    [ObservableProperty] private int _trackingCount;

    [ObservableProperty] private string _subscriptionStatusDisplay = "";
    [ObservableProperty] private string _subscriptionDetails = "";
    [ObservableProperty] private string _complimentaryDisplay = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasComplimentaryAccess))]
    private bool _hasComplimentaryAccessFlag;

    public bool HasComplimentaryAccess => HasComplimentaryAccessFlag;

    /// <summary>
    /// Доступные роли для выбора. Заполняется в LoadAsync в зависимости
    /// от роли текущего юзера: Admin видит только RegularUser/Manager,
    /// SuperAdmin видит RegularUser/Manager/Admin/SuperAdmin.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<string> _roles = new[] { "RegularUser", "Manager" };

    [ObservableProperty] private string _selectedRole = "RegularUser";

    /// <summary>
    /// Может ли текущий юзер что-то менять у этого target'а. Если target=Admin
    /// и я не SuperAdmin — все действия скрыты (бэк отдаёт 403 в любом случае).
    /// </summary>
    [ObservableProperty]
    private bool _canManageTarget = true;

    /// <summary>
    /// Может ли текущий юзер удалить target'а навсегда. Только SuperAdmin
    /// и только если target не Admin/SuperAdmin и не сам SuperAdmin.
    /// Backend дополнительно проверит (Roles=SuperAdmin + use case guards).
    /// </summary>
    [ObservableProperty]
    private bool _canDeleteTarget;

    [ObservableProperty] private string _complimentaryNote = "";

    /// <summary>
    /// Публичный wrapper для перезагрузки данных юзера. Используется
    /// AdminUserDetailsPage.OnAppearing после возврата со вложенных
    /// страниц (admin-user-tracked, и т.д.).
    /// </summary>
    public Task RefreshAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        if (!Guid.TryParse(UserId, out var id)) return;
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var envelope = await adminApi.GetUserDetailsAsync(id);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить пользователя.";
                return;
            }
            var u = envelope.Result;
            Email = u.Email;
            DisplayName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName! : u.UserName;
            Role = u.Role;
            SelectedRole = u.Role;
            Registered = u.RegisteredAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            TrackingCount = u.TrackingCount;

            // Подгружаем СВОЮ роль чтобы вычислить допустимые действия.
            // Зеркало server-side guards в ChangeRole / RevokeSubscription /
            // Complimentary — здесь только UX-гейт (бэк всё равно отдаст 403).
            var me = await authService.GetCurrentUserAsync();
            var isSuperAdmin = me is not null &&
                string.Equals(me.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            // SuperAdmin может назначать RegularUser/Manager/Admin (SuperAdmin
            // намеренно НЕ в списке — этот аккаунт через UI не выдаётся,
            // только через bootstrap-скрипт). Обычный Admin — RegularUser/Manager.
            Roles = isSuperAdmin
                ? new[] { "RegularUser", "Manager", "Admin" }
                : new[] { "RegularUser", "Manager" };

            // Если я обычный Admin и target — другой Admin / SuperAdmin,
            // все действия (роль, подписка, complimentary) скрываем.
            var targetIsAdmin = string.Equals(u.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(u.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            var targetIsSuperAdmin = string.Equals(u.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            CanManageTarget = isSuperAdmin || !targetIsAdmin;

            // Удалить юзера может ТОЛЬКО SuperAdmin. SuperAdmin (включая
            // самого себя) удалить нельзя — backend дополнительно отрежет
            // через DeleteSelfForbidden / DeleteSuperAdminForbidden.
            CanDeleteTarget = isSuperAdmin && !targetIsSuperAdmin && u.Id != (me?.Id ?? Guid.Empty);

            SubscriptionStatusDisplay = u.SubscriptionStatus switch
            {
                "None" => "Нет подписки",
                "Trial" => "Пробный период",
                "PendingPayment" => "Ожидает оплаты",
                "Active" => "Активна",
                "Cancelled" => "Отменена (платный период идёт)",
                "Expired" => "Истекла",
                _ => u.SubscriptionStatus,
            };
            SubscriptionDetails = BuildSubscriptionDetails(u);
            HasComplimentaryAccessFlag = u.HasComplimentaryAccess;
            ComplimentaryDisplay = BuildComplimentaryDisplay(u);
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ChangeRoleAsync()
    {
        if (!Guid.TryParse(UserId, out var id)) return;
        if (string.Equals(SelectedRole, Role, StringComparison.Ordinal))
        {
            StatusMessage = "Роль не изменилась.";
            return;
        }
        try
        {
            IsBusyAction = true;
            ErrorMessage = null;
            StatusMessage = null;
            var resp = await adminApi.ChangeRoleAsync(id, new ChangeRoleRequest(SelectedRole));
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"Не удалось изменить роль (HTTP {(int)resp.StatusCode}).";
                return;
            }
            Role = SelectedRole;
            StatusMessage = $"Новая роль: {SelectedRole}.";
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsBusyAction = false; }
    }

    [RelayCommand]
    private async Task GrantComplimentaryAsync()
    {
        if (!Guid.TryParse(UserId, out var id)) return;
        try
        {
            IsBusyAction = true;
            ErrorMessage = null;
            StatusMessage = null;
            var resp = await adminApi.GrantComplimentaryAsync(id,
                new GrantComplimentaryRequest(null, string.IsNullOrWhiteSpace(ComplimentaryNote) ? null : ComplimentaryNote.Trim()));
            if (resp.IsSuccessStatusCode)
            {
                StatusMessage = "Бесплатный доступ выдан (бессрочно).";
                await LoadAsync();
            }
            else
            {
                ErrorMessage = $"Ошибка (HTTP {(int)resp.StatusCode}).";
            }
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsBusyAction = false; }
    }

    [RelayCommand]
    private async Task RevokeComplimentaryAsync()
    {
        if (!Guid.TryParse(UserId, out var id)) return;
        try
        {
            IsBusyAction = true;
            ErrorMessage = null;
            StatusMessage = null;
            var resp = await adminApi.RevokeComplimentaryAsync(id);
            if (resp.IsSuccessStatusCode)
            {
                StatusMessage = "Бесплатный доступ отозван.";
                await LoadAsync();
            }
            else
            {
                ErrorMessage = $"Ошибка (HTTP {(int)resp.StatusCode}).";
            }
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsBusyAction = false; }
    }

    [RelayCommand]
    private async Task RevokeSubscriptionAsync()
    {
        if (!Guid.TryParse(UserId, out var id)) return;
        var page = Shell.Current?.CurrentPage;
        if (page is null) return;
        var confirmed = await page.DisplayAlertAsync(
            "Снять подписку?",
            "Подписка пользователя будет немедленно отозвана (статус Expired). Доступ к функциям закроется при следующем запросе.",
            "Снять",
            "Отмена");
        if (!confirmed) return;

        try
        {
            IsBusyAction = true;
            ErrorMessage = null;
            StatusMessage = null;
            var resp = await adminApi.RevokeSubscriptionAsync(id);
            if (resp.IsSuccessStatusCode)
            {
                StatusMessage = "Подписка снята.";
                await LoadAsync();
            }
            else
            {
                ErrorMessage = $"Ошибка (HTTP {(int)resp.StatusCode}).";
            }
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsBusyAction = false; }
    }

    [RelayCommand]
    private async Task RestartTrialAsync()
    {
        if (!Guid.TryParse(UserId, out var id)) return;
        var page = Shell.Current?.CurrentPage;
        if (page is null) return;
        var confirmed = await page.DisplayAlertAsync(
            "Восстановить пробный период?",
            "Пользователю будет выдан 30-дневный пробный период (Trial). Текущий статус подписки заменится.",
            "Восстановить",
            "Отмена");
        if (!confirmed) return;

        try
        {
            IsBusyAction = true;
            ErrorMessage = null;
            StatusMessage = null;
            // DurationDays=null → бэк возьмёт SubscriptionOptions.TrialDurationDays (30).
            var resp = await adminApi.RestartTrialAsync(id, new RestartTrialRequest(null));
            if (resp.IsSuccessStatusCode)
            {
                StatusMessage = "Пробный период восстановлен (30 дней).";
                await LoadAsync();
            }
            else
            {
                ErrorMessage = $"Ошибка (HTTP {(int)resp.StatusCode}).";
            }
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsBusyAction = false; }
    }

    /// <summary>Открыть список отслеживаемых для этого юзера.</summary>
    [RelayCommand]
    private async Task OpenTrackedAsync()
    {
        if (!Guid.TryParse(UserId, out _)) return;
        await Shell.Current.GoToAsync($"admin-user-tracked?userId={UserId}");
    }

    /// <summary>
    /// Жёсткое удаление юзера. Видимость кнопки — через CanDeleteTarget
    /// (только SuperAdmin). Сами защиты на бэке: Roles=SuperAdmin +
    /// DeleteSelfForbidden + DeleteSuperAdminForbidden.
    /// </summary>
    [RelayCommand]
    private async Task DeleteUserAsync()
    {
        if (!Guid.TryParse(UserId, out var id)) return;
        var page = Shell.Current?.CurrentPage;
        if (page is null) return;
        var confirmed = await page.DisplayAlertAsync(
            "Удалить пользователя навсегда?",
            $"Юзер {Email} будет удалён вместе со всеми отслеживаниями, платежами и историей. Действие необратимо.",
            "Удалить",
            "Отмена");
        if (!confirmed) return;

        try
        {
            IsBusyAction = true;
            ErrorMessage = null;
            StatusMessage = null;
            var resp = await adminApi.DeleteUserAsync(id);
            if (resp.IsSuccessStatusCode)
            {
                // Возврат на список юзеров (карточка уже не существует).
                await Shell.Current!.GoToAsync("..");
            }
            else
            {
                ErrorMessage = (int)resp.StatusCode switch
                {
                    403 => "Удалять пользователей может только SuperAdmin.",
                    404 => "Пользователь уже удалён.",
                    _ => $"Не удалось удалить (HTTP {(int)resp.StatusCode})."
                };
            }
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsBusyAction = false; }
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");

    private static string BuildSubscriptionDetails(AdminUserDetailsDto u)
    {
        var plan = string.IsNullOrWhiteSpace(u.SubscriptionPlan) ? null : u.SubscriptionPlan;
        var expires = u.SubscriptionExpiresAtUtc?.ToLocalTime().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var parts = new List<string>();
        if (plan is not null) parts.Add($"План: {plan}");
        if (expires is not null) parts.Add($"До: {expires}");
        return parts.Count == 0 ? "" : string.Join(" · ", parts);
    }

    private static string BuildComplimentaryDisplay(AdminUserDetailsDto u)
    {
        if (!u.HasComplimentaryAccess) return "Нет";
        var until = u.ComplimentaryAccessUntilUtc?.ToLocalTime().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var head = until is null ? "Бессрочно" : $"До {until}";
        return string.IsNullOrWhiteSpace(u.ComplimentaryAccessNote)
            ? head
            : $"{head} — {u.ComplimentaryAccessNote}";
    }
}
