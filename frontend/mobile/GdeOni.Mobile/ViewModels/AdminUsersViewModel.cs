using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>F17.9 mobile. Список юзеров для админа: поиск + пагинация.</summary>
public partial class AdminUsersViewModel(IAdminApi adminApi) : ObservableObject
{
    private const int PageSize = 20;
    private int _nextPage = 1;

    [ObservableProperty] private string _searchText = "";

    /// <summary>
    /// Фильтр по роли. "Все" → null на бэк, иначе строка enum'а UserRole.
    /// </summary>
    public IReadOnlyList<string> RoleFilterOptions { get; } =
        new[] { "Все", "RegularUser", "Manager", "Admin" };

    [ObservableProperty] private string _selectedRoleFilter = "Все";

    /// <summary>
    /// Фильтр по дате регистрации — диапазон От/До. DatePicker в MAUI
    /// не nullable, поэтому используем Switch IsDateFilterEnabled
    /// для включения/выключения фильтра. Когда выключен — оба null
    /// уходят на бэк (filter не применяется).
    /// </summary>
    [ObservableProperty] private DateTime _registeredFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _registeredTo = DateTime.Today;

    [ObservableProperty] private bool _isDateFilterEnabled;

    /// <summary>
    /// При смене роли в Picker'е — сразу перезагружаем первую страницу
    /// под новый фильтр, чтобы не требовалось дополнительно жать "Найти".
    /// </summary>
    partial void OnSelectedRoleFilterChanged(string value)
    {
        if (!HasLoadedOnce) return; // первичная инициализация — не дёргаем.
        _ = LoadFirstPageAsync();
    }

    partial void OnRegisteredFromChanged(DateTime value)
    {
        if (!HasLoadedOnce || !IsDateFilterEnabled) return;
        _ = LoadFirstPageAsync();
    }

    partial void OnRegisteredToChanged(DateTime value)
    {
        if (!HasLoadedOnce || !IsDateFilterEnabled) return;
        _ = LoadFirstPageAsync();
    }

    partial void OnIsDateFilterEnabledChanged(bool value)
    {
        if (!HasLoadedOnce) return;
        _ = LoadFirstPageAsync();
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public ObservableCollection<AdminUserItem> Users { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoItems))]
    private bool _hasLoadedOnce;

    public bool HasNoItems => HasLoadedOnce && Users.Count == 0 && !HasError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadMore))]
    private int _totalCount;

    public bool CanLoadMore => Users.Count < TotalCount && !IsLoading;

    [RelayCommand]
    public async Task LoadFirstPageAsync()
    {
        Users.Clear();
        _nextPage = 1;
        TotalCount = 0;
        HasLoadedOnce = false;
        await LoadNextPageAsync();
    }

    [RelayCommand]
    private async Task LoadNextPageAsync()
    {
        if (IsLoading) return;
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var role = SelectedRoleFilter == "Все" ? null : SelectedRoleFilter;
            // DatePicker отдаёт DateTime в local — переводим в UTC по дате.
            DateTime? fromUtc = IsDateFilterEnabled
                ? DateTime.SpecifyKind(RegisteredFrom.Date, DateTimeKind.Utc)
                : null;
            DateTime? toUtc = IsDateFilterEnabled
                ? DateTime.SpecifyKind(RegisteredTo.Date, DateTimeKind.Utc)
                : null;
            var envelope = await adminApi.GetUsersAsync(
                _nextPage, PageSize, search, role, fromUtc, toUtc);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить пользователей.";
                return;
            }

            foreach (var u in envelope.Result.Items)
                Users.Add(AdminUserItem.From(u));

            TotalCount = envelope.Result.TotalCount;
            _nextPage++;
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
            HasLoadedOnce = true;
            OnPropertyChanged(nameof(HasNoItems));
            OnPropertyChanged(nameof(CanLoadMore));
        }
    }

    [RelayCommand]
    private async Task OpenUserAsync(AdminUserItem? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"admin-user-details?userId={item.Id}");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

public sealed class AdminUserItem
{
    public Guid Id { get; init; }
    public string Email { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Role { get; init; } = "";
    public string Registered { get; init; } = "";
    public int TrackingCount { get; init; }

    public static AdminUserItem From(AdminUserListItem dto) => new()
    {
        Id = dto.Id,
        Email = dto.Email,
        DisplayName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName! : dto.UserName,
        Role = dto.Role,
        Registered = dto.RegisteredAtUtc.ToLocalTime().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
        TrackingCount = dto.TrackingCount,
    };
}
