using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// Админ-список отслеживаний конкретного юзера: удаление одного или всех.
/// Открывается из AdminUserDetailsPage кнопкой "Управление отслеживаниями".
/// </summary>
[QueryProperty(nameof(UserId), "userId")]
public partial class AdminUserTrackedViewModel(IAdminApi adminApi) : ObservableObject
{
    private const int PageSize = 50;
    private int _nextPage = 1;

    [ObservableProperty] private string _userId = "";

    partial void OnUserIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = LoadFirstPageAsync();
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

    public ObservableCollection<TrackedEntry> Items { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoItems))]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    private bool _hasLoadedOnce;

    public bool HasNoItems => HasLoadedOnce && Items.Count == 0 && !HasError;
    public bool HasItems => Items.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadMore))]
    private int _totalCount;

    public bool CanLoadMore => Items.Count < TotalCount && !IsLoading;

    public async Task LoadFirstPageAsync()
    {
        Items.Clear();
        _nextPage = 1;
        TotalCount = 0;
        HasLoadedOnce = false;
        await LoadNextPageAsync();
    }

    [RelayCommand]
    private async Task LoadNextPageAsync()
    {
        if (!Guid.TryParse(UserId, out var id) || IsLoading) return;
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var envelope = await adminApi.GetUserTrackedAsync(id, _nextPage, PageSize);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить отслеживания.";
                return;
            }
            foreach (var item in envelope.Result.Items)
                Items.Add(TrackedEntry.From(item));
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
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(CanLoadMore));
        }
    }

    [RelayCommand]
    private async Task RemoveOneAsync(TrackedEntry? entry)
    {
        if (entry is null || !Guid.TryParse(UserId, out var id)) return;
        var page = Shell.Current?.CurrentPage;
        if (page is null) return;
        var confirmed = await page.DisplayAlertAsync(
            "Снять отслеживание?",
            $"Удалить отслеживание {entry.FullName} у пользователя? Сама карточка умершего не пострадает.",
            "Снять",
            "Отмена");
        if (!confirmed) return;

        try
        {
            IsBusyAction = true;
            ErrorMessage = null;
            StatusMessage = null;
            var resp = await adminApi.RemoveUserTrackingAsync(id, entry.DeceasedId);
            if (resp.IsSuccessStatusCode)
            {
                Items.Remove(entry);
                TotalCount = Math.Max(0, TotalCount - 1);
                StatusMessage = "Отслеживание снято.";
                OnPropertyChanged(nameof(HasNoItems));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(CanLoadMore));
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
    private async Task RemoveAllAsync()
    {
        if (!Guid.TryParse(UserId, out var id)) return;
        var page = Shell.Current?.CurrentPage;
        if (page is null) return;
        var confirmed = await page.DisplayAlertAsync(
            "Снять все отслеживания?",
            $"Будут удалены ВСЕ отслеживания юзера ({TotalCount} шт). Сами карточки умерших не пострадают. Действие необратимо.",
            "Снять все",
            "Отмена");
        if (!confirmed) return;

        try
        {
            IsBusyAction = true;
            ErrorMessage = null;
            StatusMessage = null;
            var envelope = await adminApi.RemoveAllUserTrackingAsync(id);
            if (envelope.Result is not null)
            {
                StatusMessage = $"Снято отслеживаний: {envelope.Result.RemovedCount}.";
                Items.Clear();
                TotalCount = 0;
                OnPropertyChanged(nameof(HasNoItems));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(CanLoadMore));
            }
            else
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось снять.";
            }
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsBusyAction = false; }
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

public sealed class TrackedEntry
{
    public Guid DeceasedId { get; init; }
    public string FullName { get; init; } = "";
    public string LifePeriod { get; init; } = "";
    public string Relationship { get; init; } = "";
    public string Status { get; init; } = "";
    public string TrackedAt { get; init; } = "";

    public static TrackedEntry From(AdminUserTrackedItem dto)
    {
        var birth = dto.BirthDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "?";
        var death = dto.DeathDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        return new TrackedEntry
        {
            DeceasedId = dto.DeceasedId,
            FullName = dto.FullName,
            LifePeriod = $"{birth} — {death}",
            Relationship = dto.RelationshipType,
            Status = dto.Status,
            TrackedAt = dto.TrackedAtUtc.ToLocalTime().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
        };
    }
}
