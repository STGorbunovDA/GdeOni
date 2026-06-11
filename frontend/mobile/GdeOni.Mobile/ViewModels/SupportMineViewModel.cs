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
/// D25 mobile. Лента "Мои обращения". Юзер видит свои Manual-тикеты
/// + auto-инциденты с привязкой к нему. Сортировка DESC по дате.
/// </summary>
public partial class SupportMineViewModel(ISupportApi supportApi) : ObservableObject
{
    private const int PageSize = 20;
    private int _nextPage = 1;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public ObservableCollection<SupportTicketEntry> Tickets { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoItems))]
    private bool _hasLoadedOnce;

    public bool HasNoItems => HasLoadedOnce && Tickets.Count == 0 && !HasError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadMore))]
    private int _totalCount;

    public bool CanLoadMore => Tickets.Count < TotalCount && !IsLoading;

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
            var envelope = await supportApi.GetMineAsync(_nextPage, PageSize);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить обращения.";
                return;
            }
            foreach (var t in envelope.Result.Items)
                Tickets.Add(SupportTicketEntry.From(t));
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
    private async Task OpenAsync(SupportTicketEntry? entry)
    {
        if (entry is null) return;
        await Shell.Current.GoToAsync($"support-details?ticketId={entry.Id}");
    }

    [RelayCommand]
    private async Task NewAsync() => await Shell.Current.GoToAsync("support-new");

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

public sealed class SupportTicketEntry
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string KindDisplay { get; init; } = "";
    public string StatusDisplay { get; init; } = "";
    public string StatusColor { get; init; } = "#000";
    public string SeverityDisplay { get; init; } = "";
    public string SeverityColor { get; init; } = "#000";
    public string CreatedAt { get; init; } = "";

    public static SupportTicketEntry From(SupportTicketDto dto)
    {
        var (statusDisplay, statusColor) = SupportDisplay.Status(dto.Status);
        var (severityDisplay, severityColor) = SupportDisplay.Severity(dto.Severity);
        return new SupportTicketEntry
        {
            Id = dto.Id,
            Title = dto.Title,
            KindDisplay = SupportDisplay.Kind(dto.Kind),
            StatusDisplay = statusDisplay,
            StatusColor = statusColor,
            SeverityDisplay = severityDisplay,
            SeverityColor = severityColor,
            CreatedAt = dto.CreatedAtUtc.ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
        };
    }
}
