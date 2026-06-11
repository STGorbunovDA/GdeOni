using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.ViewModels.Support;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// D25 mobile. Админский список обращений с фильтрами. Чек-боксы
/// статусов и severity позволяют выбрать несколько одновременно
/// (например, "Open + InProgress + Urgent").
/// </summary>
public partial class AdminSupportViewModel(ISupportApi supportApi) : ObservableObject
{
    private const int PageSize = 20;
    private int _nextPage = 1;

    // ───────── Чек-боксы статусов ─────────
    [ObservableProperty] private bool _filterStatusOpen = true;
    [ObservableProperty] private bool _filterStatusInProgress = true;
    [ObservableProperty] private bool _filterStatusResolved;

    // ───────── Чек-боксы severity ─────────
    [ObservableProperty] private bool _filterSeverityNormal = true;
    [ObservableProperty] private bool _filterSeverityUrgent = true;

    // ───────── Picker'ы для kind / source ─────────
    public IReadOnlyList<string> KindFilterOptions { get; } =
        new[] { "Все", "Платёж", "Ошибка / Баг", "Жалоба", "Вопрос", "Другое" };
    public IReadOnlyList<string> SourceFilterOptions { get; } =
        new[] { "Все", "От пользователя", "Авто-инцидент" };

    [ObservableProperty] private string _selectedKindFilter = "Все";
    [ObservableProperty] private string _selectedSourceFilter = "Все";

    // ───────── Поиск ─────────
    [ObservableProperty] private string _search = "";

    // ───────── Состояние ─────────
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<AdminSupportEntry> Tickets { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoItems))]
    private bool _hasLoadedOnce;
    public bool HasNoItems => HasLoadedOnce && Tickets.Count == 0 && !HasError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadMore))]
    private int _totalCount;
    public bool CanLoadMore => Tickets.Count < TotalCount && !IsLoading;

    // Перезагружаем при любой смене фильтра — но только если уже
    // была первая загрузка (иначе первый автозапуск из OnAppearing
    // дёргается несколько раз).
    partial void OnFilterStatusOpenChanged(bool value) => RefilterIfReady();
    partial void OnFilterStatusInProgressChanged(bool value) => RefilterIfReady();
    partial void OnFilterStatusResolvedChanged(bool value) => RefilterIfReady();
    partial void OnFilterSeverityNormalChanged(bool value) => RefilterIfReady();
    partial void OnFilterSeverityUrgentChanged(bool value) => RefilterIfReady();
    partial void OnSelectedKindFilterChanged(string value) => RefilterIfReady();
    partial void OnSelectedSourceFilterChanged(string value) => RefilterIfReady();

    private void RefilterIfReady()
    {
        if (!HasLoadedOnce) return;
        _ = LoadFirstPageAsync();
    }

    [RelayCommand]
    private async Task ApplySearchAsync() => await LoadFirstPageAsync();

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        FilterStatusOpen = true;
        FilterStatusInProgress = true;
        FilterStatusResolved = false;
        FilterSeverityNormal = true;
        FilterSeverityUrgent = true;
        SelectedKindFilter = "Все";
        SelectedSourceFilter = "Все";
        Search = "";
        await LoadFirstPageAsync();
    }

    [RelayCommand]
    public async Task LoadFirstPageAsync()
    {
        Tickets.Clear();
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

            var statuses = BuildStatuses();
            var severities = BuildSeverities();
            var kindCode = MapKindLabel(SelectedKindFilter);
            var sourceCode = MapSourceLabel(SelectedSourceFilter);
            var searchTerm = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();

            var envelope = await supportApi.GetAdminAsync(
                page: _nextPage,
                pageSize: PageSize,
                statuses: statuses,
                severities: severities,
                kind: kindCode,
                source: sourceCode,
                search: searchTerm);

            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить обращения.";
                return;
            }

            foreach (var t in envelope.Result.Items)
                Tickets.Add(AdminSupportEntry.From(t));
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
    private async Task OpenAsync(AdminSupportEntry? entry)
    {
        if (entry is null) return;
        await Shell.Current.GoToAsync($"admin-support-details?ticketId={entry.Id}");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");

    private string[]? BuildStatuses()
    {
        var list = new List<string>();
        if (FilterStatusOpen) list.Add("Open");
        if (FilterStatusInProgress) list.Add("InProgress");
        if (FilterStatusResolved) list.Add("Resolved");
        // Если все чек-боксы выключены — не шлём фильтр вообще (равно
        // "показать все"). Если все три включены — тоже null (бэк без
        // фильтра дешевле, чем IN с 3 значениями).
        if (list.Count == 0 || list.Count == 3) return null;
        return list.ToArray();
    }

    private string[]? BuildSeverities()
    {
        var list = new List<string>();
        if (FilterSeverityNormal) list.Add("Normal");
        if (FilterSeverityUrgent) list.Add("Urgent");
        if (list.Count == 0 || list.Count == 2) return null;
        return list.ToArray();
    }

    private static string? MapKindLabel(string label) => label switch
    {
        "Все" => null,
        "Платёж" => "Payment",
        "Ошибка / Баг" => "Bug",
        "Жалоба" => "Complaint",
        "Вопрос" => "Question",
        "Другое" => "Other",
        _ => null,
    };

    private static string? MapSourceLabel(string label) => label switch
    {
        "Все" => null,
        "От пользователя" => "Manual",
        "Авто-инцидент" => "Auto",
        _ => null,
    };
}

public sealed class AdminSupportEntry
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string KindDisplay { get; init; } = "";
    public string SourceDisplay { get; init; } = "";
    public string StatusDisplay { get; init; } = "";
    public string StatusColor { get; init; } = "#000";
    public string SeverityDisplay { get; init; } = "";
    public string SeverityColor { get; init; } = "#000";
    public string CreatedAt { get; init; } = "";
    public string UserEmail { get; init; } = "";

    public static AdminSupportEntry From(SupportTicketDto dto)
    {
        var (sd, sc) = SupportDisplay.Status(dto.Status);
        var (sevd, sevc) = SupportDisplay.Severity(dto.Severity);
        return new AdminSupportEntry
        {
            Id = dto.Id,
            Title = dto.Title,
            KindDisplay = SupportDisplay.Kind(dto.Kind),
            SourceDisplay = SupportDisplay.Source(dto.Source),
            StatusDisplay = sd,
            StatusColor = sc,
            SeverityDisplay = sevd,
            SeverityColor = sevc,
            CreatedAt = dto.CreatedAtUtc.ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            UserEmail = dto.UserEmail ?? "— (не определён)",
        };
    }
}
