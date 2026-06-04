using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// F17.9 mobile. История всех платежей подписки. Фильтра пока нет —
/// тапнул "Загрузить ещё" → доскроллил.
/// </summary>
public partial class AdminPaymentsViewModel(IAdminApi adminApi) : ObservableObject
{
    private const int PageSize = 20;
    private int _nextPage = 1;

    /// <summary>Все возможные статусы + "Все" sentinel для отсутствия фильтра.</summary>
    public IReadOnlyList<string> StatusFilterOptions { get; } =
        new[] { "Все", "Pending", "Succeeded", "Cancelled", "Failed" };

    [ObservableProperty] private string _selectedStatusFilter = "Все";

    partial void OnSelectedStatusFilterChanged(string value)
    {
        if (!HasLoadedOnce) return;
        _ = LoadFirstPageAsync();
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public ObservableCollection<AdminPaymentEntry> Payments { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoItems))]
    private bool _hasLoadedOnce;

    public bool HasNoItems => HasLoadedOnce && Payments.Count == 0 && !HasError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadMore))]
    private int _totalCount;

    public bool CanLoadMore => Payments.Count < TotalCount && !IsLoading;

    [RelayCommand]
    public async Task LoadFirstPageAsync()
    {
        Payments.Clear();
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
            var status = SelectedStatusFilter == "Все" ? null : SelectedStatusFilter;
            var envelope = await adminApi.GetPaymentsAsync(_nextPage, PageSize, status);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить платежи.";
                return;
            }
            foreach (var p in envelope.Result.Items)
                Payments.Add(AdminPaymentEntry.From(p));
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
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

public sealed class AdminPaymentEntry
{
    public string CreatedAt { get; init; } = "";
    public string UserEmail { get; init; } = "";
    public string Amount { get; init; } = "";
    public string StatusDisplay { get; init; } = "";
    public string StatusColor { get; init; } = "#000";
    public string Period { get; init; } = "";
    public string Plan { get; init; } = "";

    public static AdminPaymentEntry From(AdminPaymentItem dto)
    {
        var (statusDisplay, color) = dto.Status switch
        {
            "Succeeded" => ("Успешно", "#2E7D32"),
            "Pending" => ("Ожидание", "#F9A825"),
            "Cancelled" => ("Отменён", "#7F8C8D"),
            "Failed" => ("Ошибка", "#C0392B"),
            _ => (dto.Status, "#000")
        };

        var period = dto.PeriodStartUtc is { } ps && dto.PeriodEndUtc is { } pe
            ? $"{ps.ToLocalTime():dd.MM.yyyy}–{pe.ToLocalTime():dd.MM.yyyy}"
            : "—";

        return new AdminPaymentEntry
        {
            CreatedAt = dto.CreatedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            UserEmail = dto.UserEmail ?? "—",
            Amount = $"{dto.AmountRub:0.##} ₽",
            StatusDisplay = statusDisplay,
            StatusColor = color,
            Period = period,
            Plan = dto.Plan,
        };
    }
}
