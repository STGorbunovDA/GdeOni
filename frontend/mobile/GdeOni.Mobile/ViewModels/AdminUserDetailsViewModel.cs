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

    /// <summary>Доступные роли — SuperAdmin сам себе не меняет.</summary>
    public IReadOnlyList<string> Roles { get; } = new[] { "Standard", "Admin", "SuperAdmin" };

    [ObservableProperty] private string _selectedRole = "Standard";
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
            StatusMessage = resp.IsSuccessStatusCode
                ? "Бесплатный доступ выдан (бессрочно)."
                : $"Ошибка (HTTP {(int)resp.StatusCode}).";
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
            StatusMessage = resp.IsSuccessStatusCode
                ? "Бесплатный доступ отозван."
                : $"Ошибка (HTTP {(int)resp.StatusCode}).";
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsBusyAction = false; }
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}
