using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// D27. Админский поиск умерших по всем характеристикам. В отличие
/// от юзерского поиска (DeceasedSearchPage → preview → "добавить в
/// отслеживание"), здесь тап по карточке открывает полный admin-вид
/// без необходимости подписываться. Используется для управления
/// чужими карточками: добавить фото/документ, поменять координаты,
/// верифицировать.
/// </summary>
public partial class AdminFindDeceasedViewModel(IDeceasedRecordsApi deceasedApi) : ObservableObject
{
    private const int PageSize = 20;
    private int _nextPage = 1;

    // ───────── Текстовые фильтры ─────────
    [ObservableProperty] private string _search = "";
    [ObservableProperty] private string _firstName = "";
    [ObservableProperty] private string _lastName = "";
    [ObservableProperty] private string _middleName = "";
    [ObservableProperty] private string _country = "";
    [ObservableProperty] private string _city = "";

    // ───────── Даты ─────────
    // Используем "включён ли фильтр" + DatePicker — иначе DatePicker
    // всегда отдаёт какое-то значение и фильтр не убрать.
    [ObservableProperty] private bool _filterByBirthDate;
    [ObservableProperty] private DateTime _birthDate = DateTime.Today.AddYears(-50);

    [ObservableProperty] private bool _filterByDeathDate;
    [ObservableProperty] private DateTime _deathDate = DateTime.Today;

    [ObservableProperty] private bool _filterByCreatedFrom;
    [ObservableProperty] private DateTime _createdFrom = DateTime.Today.AddMonths(-1);

    [ObservableProperty] private bool _filterByCreatedTo;
    [ObservableProperty] private DateTime _createdTo = DateTime.Today;

    // ───────── Верификация ─────────
    // Picker "Все / Только проверенные / Только непроверенные" —
    // вместо тривиального CheckBox, чтобы можно было искать
    // непроверенные кандидаты на верификацию.
    public IReadOnlyList<string> VerifiedOptions { get; } =
        new[] { "Все", "Только проверенные", "Только непроверенные" };
    [ObservableProperty] private string _selectedVerifiedOption = "Все";

    // ───────── Состояние ─────────
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<DeceasedSearchItem> Items { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoItems))]
    private bool _hasLoadedOnce;
    public bool HasNoItems => HasLoadedOnce && Items.Count == 0 && !HasError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadMore))]
    private int _totalCount;
    public bool CanLoadMore => Items.Count < TotalCount && !IsLoading;

    // Реагируем на toggle фильтра и Picker — текстовые поля
    // перезагружают по кнопке "Найти" (иначе на каждый символ — запрос).
    partial void OnFilterByBirthDateChanged(bool value) => RefilterIfReady();
    partial void OnFilterByDeathDateChanged(bool value) => RefilterIfReady();
    partial void OnFilterByCreatedFromChanged(bool value) => RefilterIfReady();
    partial void OnFilterByCreatedToChanged(bool value) => RefilterIfReady();
    partial void OnSelectedVerifiedOptionChanged(string value) => RefilterIfReady();
    partial void OnBirthDateChanged(DateTime value) { if (FilterByBirthDate) RefilterIfReady(); }
    partial void OnDeathDateChanged(DateTime value) { if (FilterByDeathDate) RefilterIfReady(); }
    partial void OnCreatedFromChanged(DateTime value) { if (FilterByCreatedFrom) RefilterIfReady(); }
    partial void OnCreatedToChanged(DateTime value) { if (FilterByCreatedTo) RefilterIfReady(); }

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
        Search = "";
        FirstName = "";
        LastName = "";
        MiddleName = "";
        Country = "";
        City = "";
        FilterByBirthDate = false;
        FilterByDeathDate = false;
        FilterByCreatedFrom = false;
        FilterByCreatedTo = false;
        SelectedVerifiedOption = "Все";
        await LoadFirstPageAsync();
    }

    [RelayCommand]
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
        if (IsLoading) return;
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var envelope = await deceasedApi.GetAllAsync(
                search: NullIfBlank(Search),
                firstName: NullIfBlank(FirstName),
                lastName: NullIfBlank(LastName),
                middleName: NullIfBlank(MiddleName),
                country: NullIfBlank(Country),
                city: NullIfBlank(City),
                birthDate: FilterByBirthDate ? FormatDate(BirthDate) : null,
                deathDate: FilterByDeathDate ? FormatDate(DeathDate) : null,
                isVerified: MapVerifiedOption(SelectedVerifiedOption),
                createdFrom: FilterByCreatedFrom ? FormatDateTime(CreatedFrom) : null,
                createdTo: FilterByCreatedTo ? FormatDateTime(CreatedTo.AddDays(1).AddSeconds(-1)) : null,
                page: _nextPage,
                pageSize: PageSize);

            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить список.";
                HasLoadedOnce = true;
                return;
            }

            foreach (var item in envelope.Result.Items)
                Items.Add(item);

            TotalCount = envelope.Result.TotalCount;
            _nextPage++;
            HasLoadedOnce = true;
            OnPropertyChanged(nameof(HasNoItems));
            OnPropertyChanged(nameof(CanLoadMore));
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
            HasLoadedOnce = true;
        }
        catch (HttpRequestException httpEx)
        {
            ErrorMessage = $"Сетевая ошибка: {httpEx.Message}";
            HasLoadedOnce = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
            HasLoadedOnce = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Тап по карточке умершего → admin-view (полный просмотр + админ-
    /// действия). Намеренно НЕ открывает обычный deceased-details, чтобы
    /// не создавать tracking-запись и не показывать кнопки "Добавить в
    /// отслеживание".
    /// </summary>
    [RelayCommand]
    private async Task OpenAsync(DeceasedSearchItem? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"admin-deceased-view?deceasedId={item.Id}");
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        try { await Shell.Current.GoToAsync(".."); }
        catch { await Shell.Current.GoToAsync("//main/profile"); }
    }

    private static string? NullIfBlank(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string FormatDate(DateTime dt)
        => DateOnly.FromDateTime(dt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatDateTime(DateTime dt)
        => dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    private static bool? MapVerifiedOption(string option) => option switch
    {
        "Только проверенные" => true,
        "Только непроверенные" => false,
        _ => null,
    };
}
