using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Media;
using GdeOni.Mobile.Shared.Notifications;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// Вкладка «События». Сверху — годовщины сегодня среди отслеживаемых
/// (день рождения / годовщина смерти), тап ведёт на карточку умершего.
/// Ниже — праздники сегодня и ближайшие (30 дней), сгруппированные по
/// категориям (backend GET /api/events/holidays). Зеркало web EventsPage.
/// </summary>
public partial class EventsViewModel(
    ITrackedDeceasedApi trackedApi,
    IEventsApi eventsApi,
    IPublicHostsService publicHosts) : ObservableObject
{
    private const int UpcomingDays = 30;

    private static readonly Dictionary<string, (string Label, int Order)> CategoryMeta = new()
    {
        ["Memorial"] = ("Поминальные дни", 0),
        ["Orthodox"] = ("Православные", 1),
        ["Muslim"] = ("Мусульманские", 2),
        ["State"] = ("Государственные", 3),
        // D38.1. Пост — не праздник: отдельная группа, последняя в списке.
        ["Fast"] = ("Посты", 4),
    };

    [ObservableProperty]
    private string _title = "События";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<AnniversaryItemViewModel> TodayAnniversaries { get; } = new();
    public ObservableCollection<HolidayItemViewModel> TodayHolidays { get; } = new();
    public ObservableCollection<HolidayGroupViewModel> UpcomingGroups { get; } = new();

    public bool HasAnniversaries => TodayAnniversaries.Count > 0;
    public bool HasNoAnniversaries => TodayAnniversaries.Count == 0;
    public bool HasTodayHolidays => TodayHolidays.Count > 0;
    public bool HasNoTodayHolidays => TodayHolidays.Count == 0;
    public bool HasUpcoming => UpcomingGroups.Count > 0;

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var today = DateOnly.FromDateTime(DateTime.Now);

            await LoadAnniversariesAsync(today);
            await LoadHolidaysAsync(today);
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (HttpRequestException httpEx)
        {
            ErrorMessage = $"Сетевая ошибка: {httpEx.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task OpenDeceasedAsync(Guid deceasedId)
    {
        if (deceasedId == Guid.Empty) return;
        await Shell.Current.GoToAsync($"deceased-details?deceasedId={deceasedId}");
    }

    private async Task LoadAnniversariesAsync(DateOnly today)
    {
        TodayAnniversaries.Clear();

        var envelope = await trackedApi.GetListAsync(page: 1, pageSize: 100);
        if (envelope.Result is null)
            return;

        foreach (var item in envelope.Result.Items)
        {
            if (item.Status == TrackStatuses.Archived)
                continue;

            var deathYears = AnniversaryDateCalculator.YearsSinceIfToday(item.DeathDate, today);
            var birthYears = item.BirthDate is { } birth
                ? AnniversaryDateCalculator.YearsSinceIfToday(birth, today)
                : null;

            if (deathYears is null && birthYears is null)
                continue;

            var resolved = await item.ResolveMainPhotoAsync(publicHosts);

            if (deathYears is int dy)
            {
                TodayAnniversaries.Add(new AnniversaryItemViewModel(
                    resolved.DeceasedId,
                    resolved.FullName,
                    $"Година · {dy} {YearsWord(dy)}",
                    resolved.MainPhotoUrl));
            }

            if (birthYears is int by)
            {
                TodayAnniversaries.Add(new AnniversaryItemViewModel(
                    resolved.DeceasedId,
                    resolved.FullName,
                    $"День рождения · {by} {YearsWord(by)}",
                    resolved.MainPhotoUrl));
            }
        }

        OnPropertyChanged(nameof(HasAnniversaries));
        OnPropertyChanged(nameof(HasNoAnniversaries));
    }

    private async Task LoadHolidaysAsync(DateOnly today)
    {
        TodayHolidays.Clear();
        UpcomingGroups.Clear();

        var to = today.AddDays(UpcomingDays);
        var envelope = await eventsApi.GetHolidaysAsync(
            today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        var holidays = envelope.Result?.Holidays ?? Array.Empty<HolidayDto>();

        foreach (var h in holidays.Where(h => h.Date == today))
            TodayHolidays.Add(new HolidayItemViewModel(h.Name, string.Empty));

        var groups = holidays
            .Where(h => h.Date > today)
            .GroupBy(h => h.Category)
            .OrderBy(g => CategoryOrder(g.Key))
            .Select(g => new HolidayGroupViewModel(
                CategoryLabel(g.Key),
                g.OrderBy(h => h.Date)
                    .Select(h => new HolidayItemViewModel(
                        h.Name,
                        h.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)))
                    .ToList()));

        foreach (var group in groups)
            UpcomingGroups.Add(group);

        OnPropertyChanged(nameof(HasTodayHolidays));
        OnPropertyChanged(nameof(HasNoTodayHolidays));
        OnPropertyChanged(nameof(HasUpcoming));
    }

    private static int CategoryOrder(string category) =>
        CategoryMeta.TryGetValue(category, out var meta) ? meta.Order : 99;

    private static string CategoryLabel(string category) =>
        CategoryMeta.TryGetValue(category, out var meta) ? meta.Label : category;

    private static string YearsWord(int count)
    {
        var n = Math.Abs(count);
        var mod100 = n % 100;
        var mod10 = n % 10;
        if (mod100 is >= 11 and <= 14) return "лет";
        return mod10 switch
        {
            1 => "год",
            >= 2 and <= 4 => "года",
            _ => "лет",
        };
    }
}

public sealed class AnniversaryItemViewModel(
    Guid deceasedId,
    string fullName,
    string subtitle,
    string? mainPhotoUrl)
{
    public Guid DeceasedId { get; } = deceasedId;
    public string FullName { get; } = fullName;
    public string Subtitle { get; } = subtitle;
    public string? MainPhotoUrl { get; } = mainPhotoUrl;
    public bool HasPhoto => !string.IsNullOrEmpty(MainPhotoUrl);
}

public sealed class HolidayItemViewModel(string name, string dateText)
{
    public string Name { get; } = name;
    public string DateText { get; } = dateText;
    public bool HasDate => !string.IsNullOrEmpty(DateText);
}

public sealed class HolidayGroupViewModel(string categoryLabel, IReadOnlyList<HolidayItemViewModel> items)
{
    public string CategoryLabel { get; } = categoryLabel;
    public IReadOnlyList<HolidayItemViewModel> Items { get; } = items;
}
