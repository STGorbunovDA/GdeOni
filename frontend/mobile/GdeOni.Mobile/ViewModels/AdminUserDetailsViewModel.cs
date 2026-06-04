using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// F17.9 mobile. Детальная карточка юзера: смена роли + выдача/отзыв
/// бесплатного доступа. Подписку и платежи юзера не показываем —
/// для этого есть отдельный раздел "Платежи" в админке.
/// </summary>
[QueryProperty(nameof(UserId), "userId")]
public partial class AdminUserDetailsViewModel(IAdminApi adminApi) : ObservableObject
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
    /// Доступные роли — соответствуют enum UserRole на бэке. SuperAdmin
    /// исключён намеренно: его не выставляют через UI (только bootstrap
    /// через seed). Manager — для модераторов без полных прав админа.
    /// </summary>
    public IReadOnlyList<string> Roles { get; } = new[] { "RegularUser", "Manager", "Admin" };

    [ObservableProperty] private string _selectedRole = "RegularUser";
    [ObservableProperty] private string _complimentaryNote = "";

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
