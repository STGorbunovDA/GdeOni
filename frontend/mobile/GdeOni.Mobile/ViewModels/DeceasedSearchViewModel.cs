using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Shared.Search;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// E16. Экран поиска существующего умершего перед добавлением новой
/// карточки. Цель — не плодить дубликаты:
/// 1. Юзер вводит ФИО / город.
/// 2. Видит список совпадений с TotalCount и кнопкой "Загрузить ещё"
///    если результатов больше PageSize (масштабируемость: база может
///    разрастись до десятков тысяч).
/// 3. Тап на совпадение → preview-страница (E17.1), без моментальной
///    подписки. Подписаться юзер сможет уже на preview-карточке.
/// 4. Если ничего не подошло — кнопка "Создать новую карточку" →
///    обычный AtGravePage flow.
/// </summary>
public partial class DeceasedSearchViewModel(
    IDeceasedRecordsApi deceasedApi) : ObservableObject
{
    private const int PageSize = 20;

    /// <summary>
    /// Legacy "любое поле" — оставлено в VM на случай если кому-то
    /// понадобится. На текущем UI используется только три раздельных
    /// поля (FirstName/LastName/MiddleName).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private string _query = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private string _firstNameFilter = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private string _lastNameFilter = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private string _middleNameFilter = "";

    [ObservableProperty]
    private string _cityFilter = "";

    /// <summary>
    /// DatePicker в XAML не умеет хранить null — биндим к DateTime?
    /// через TargetNullValue, но проще пара (DateTime? + bool flag).
    /// Юзер сначала включает чекбокс "по дате рождения", потом выбирает
    /// дату. Без чекбокса фильтр не отправляется (null в API).
    /// </summary>
    [ObservableProperty]
    private DateTime _birthDate = DateTime.Today.AddYears(-70);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private bool _useBirthDateFilter;

    /// <summary>
    /// Default = Today. Раньше было Today-1 (год назад), но это сбивало
    /// с толку: если юзер недавно создал карточку с "сегодняшней"
    /// DeathDate и сразу же зашёл искать — год отличается на 1.
    /// </summary>
    [ObservableProperty]
    private DateTime _deathDate = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private bool _useDeathDateFilter;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShowTooManyHint))]
    [NotifyPropertyChangedFor(nameof(SummaryText))]
    [NotifyPropertyChangedFor(nameof(HasSummary))]
    private bool _hasSearched;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTooManyHint))]
    [NotifyPropertyChangedFor(nameof(SummaryText))]
    [NotifyPropertyChangedFor(nameof(HasSummary))]
    [NotifyPropertyChangedFor(nameof(HasMore))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMore))]
    private int _currentPage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Кнопка "Найти" активна если задано ХОТЯ БЫ ОДНО из:
    /// - FirstName/LastName/MiddleName/Query длиной >= 2 символов;
    /// - включён фильтр по дате рождения;
    /// - включён фильтр по дате смерти.
    /// Каждый критерий независим — можно искать только по дате,
    /// только по имени, или комбинировать. Поиск совсем без
    /// критериев перебирает всю базу — не пускаем.
    /// </summary>
    public bool CanSearch => DeceasedSearchCriteria.CanSearch(
        Query, FirstNameFilter, LastNameFilter, MiddleNameFilter,
        UseBirthDateFilter, UseDeathDateFilter);

    public ObservableCollection<DeceasedSearchItem> Results { get; } = new();
    public bool HasResults => Results.Count > 0;
    public bool ShowEmptyState => HasSearched && Results.Count == 0 && !IsBusy;

    /// <summary>
    /// Когда совпадений много (>50) — подсказываем уточнить город,
    /// чтобы юзер не скроллил тысячи однофамильцев.
    /// </summary>
    public bool ShowTooManyHint => HasSearched && TotalCount > 50;

    public string SummaryText => $"Найдено: {TotalCount}, показано: {Results.Count}";
    public bool HasSummary => HasSearched && TotalCount > 0;

    /// <summary>
    /// Есть ли ещё страницы для подгрузки. Считаем по сравнению
    /// показанных vs всех (вместо хранения lastPage явно).
    /// </summary>
    public bool HasMore => Results.Count < TotalCount;

    [RelayCommand]
    public async Task SearchAsync()
    {
        if (!CanSearch) return;

        CurrentPage = 0;
        Results.Clear();
        TotalCount = 0;
        OnPropertyChanged(nameof(HasResults));

        await LoadNextPageAsync();
    }

    /// <summary>
    /// Загрузка следующей страницы (page+1). Используется кнопкой
    /// "Загрузить ещё" после первого Search.
    /// </summary>
    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!HasMore || IsBusy) return;
        await LoadNextPageAsync();
    }

    private async Task LoadNextPageAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            HasSearched = true;

            var nextPage = CurrentPage + 1;
            var cityArg = NullIfEmpty(CityFilter);
            var firstArg = NullIfEmpty(FirstNameFilter);
            var lastArg = NullIfEmpty(LastNameFilter);
            var middleArg = NullIfEmpty(MiddleNameFilter);
            var queryArg = NullIfEmpty(Query);
            // Ручное форматирование в ISO yyyy-MM-dd через invariant culture —
            // backend парсит DateOnly детерминированно (см. комментарий
            // в IDeceasedRecordsApi.GetAllAsync).
            var birthArg = UseBirthDateFilter
                ? DateOnly.FromDateTime(BirthDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : null;
            var deathArg = UseDeathDateFilter
                ? DateOnly.FromDateTime(DeathDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : null;

            var envelope = await deceasedApi.GetAllAsync(
                search: queryArg,
                firstName: firstArg,
                lastName: lastArg,
                middleName: middleArg,
                city: cityArg,
                birthDate: birthArg,
                deathDate: deathArg,
                page: nextPage,
                pageSize: PageSize);

            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось выполнить поиск.";
                return;
            }

            foreach (var item in envelope.Result.Items)
                Results.Add(item);

            TotalCount = envelope.Result.TotalCount;
            CurrentPage = nextPage;

            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(HasMore));
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

    /// <summary>
    /// Tap на найденного — открываем preview-страницу (E17.1) вместо
    /// моментальной подписки. Юзер сначала проверяет "тот ли это
    /// умерший", и только потом нажимает "Добавить в отслеживание"
    /// уже на preview. Это защита от того, что в поиске встретился
    /// тёзка/однофамилец с такими же датами.
    /// </summary>
    [RelayCommand]
    private async Task OpenPreviewAsync(DeceasedSearchItem? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"deceased-preview?deceasedId={item.Id}");
    }

    /// <summary>
    /// "Не нашёл — создать новую карточку" → продолжение в AtGravePage.
    /// Это relative push, чтобы можно было вернуться кнопкой "Назад".
    /// </summary>
    [RelayCommand]
    private async Task CreateNewAsync()
    {
        await Shell.Current.GoToAsync("at-grave");
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("//main/tracked");
    }

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
