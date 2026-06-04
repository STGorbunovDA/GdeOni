using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Geolocation;
using GdeOni.Mobile.Shared.Search;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// E21. "Найти рядом" — пользователь на кладбище берёт GPS, отправляет
/// запрос с радиусом и получает список ближайших карточек. Тап → preview
/// (E17.1) → подписка → можно строить маршрут.
/// </summary>
public partial class NearbySearchViewModel(
    IDeceasedRecordsApi deceasedApi,
    ITrackedDeceasedApi trackedApi,
    IGeolocationService geo) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGeoError))]
    private string? _geoErrorMessage;

    [ObservableProperty]
    private bool _showOpenSettings;

    /// <summary>
    /// Радиус в метрах. Дефолт 100м — типичный сценарий "стою на участке".
    /// Слайдер на странице позволяет 50 / 100 / 200 / 500. Бэк допускает
    /// [10, 5000], дальше — full-scan и за рамками "рядом".
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RadiusDisplay))]
    private int _radiusMeters = 100;

    public string RadiusDisplay => DistanceFormatter.Format(RadiusMeters);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _hasSearched;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasGeoError => !string.IsNullOrEmpty(GeoErrorMessage);

    public ObservableCollection<NearbyDeceasedRowViewModel> Results { get; } = new();
    public bool HasResults => Results.Count > 0;
    public bool ShowEmptyState => HasSearched && !IsBusy && Results.Count == 0;

    public string SummaryText => HasSearched
        ? $"Найдено: {Results.Count} в радиусе {RadiusDisplay}"
        : string.Empty;

    public bool HasSummary => HasSearched;

    // ─── Batch-add выбранных ───
    public int SelectedCount => Results.Count(x => x.IsSelected);
    public bool HasSelection => SelectedCount > 0;
    public string AddSelectedButtonText => $"Добавить выбранных ({SelectedCount})";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAddStatus))]
    private string? _addStatusMessage;
    public bool HasAddStatus => !string.IsNullOrEmpty(AddStatusMessage);

    /// <summary>
    /// Подписываемся на IsSelected каждой row чтобы пересчитывать
    /// SelectedCount / HasSelection / текст кнопки.
    /// </summary>
    private void HookRowSelectionEvents()
    {
        foreach (var row in Results)
            row.PropertyChanged += OnRowPropertyChanged;
    }

    private void UnhookRowSelectionEvents()
    {
        foreach (var row in Results)
            row.PropertyChanged -= OnRowPropertyChanged;
    }

    private void OnRowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(NearbyDeceasedRowViewModel.IsSelected)) return;
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(AddSelectedButtonText));
    }

    /// <summary>
    /// Главная кнопка "Найти рядом": берём GPS → GET /nearby → заполняем
    /// Results. При permission-denied показываем ShowOpenSettings.
    /// </summary>
    [RelayCommand]
    public async Task SearchAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            GeoErrorMessage = null;
            ShowOpenSettings = false;

            var location = await geo.RequestAndGetCurrentAsync();
            if (!location.Success
                || location.Latitude is not double lat
                || location.Longitude is not double lon)
            {
                GeoErrorMessage = location.ErrorMessage ?? "Не удалось получить координаты.";
                ShowOpenSettings = location.ErrorKind == GeolocationErrorKind.PermissionDeniedPermanent;
                Results.Clear();
                HasSearched = true;
                OnPropertyChanged(nameof(HasResults));
                OnPropertyChanged(nameof(SummaryText));
                OnPropertyChanged(nameof(HasSummary));
                return;
            }

            var envelope = await deceasedApi.GetNearbyAsync(
                latitude: lat,
                longitude: lon,
                radiusMeters: RadiusMeters);

            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось выполнить поиск.";
                return;
            }

            // Тянем список уже отслеживаемых — чтобы пометить найденные
            // галкой и поднять их в верх списка. 200 хватит большинству;
            // если у юзера больше — те просто не будут предотмечены,
            // что не ломает UX.
            var alreadyTracked = await LoadAlreadyTrackedIdsAsync();

            UnhookRowSelectionEvents();
            Results.Clear();
            AddStatusMessage = null;

            // Сортировка: отслеживаемые сверху (в том же порядке как пришли
            // с бэка), за ними — остальные. По расстоянию бэк уже отсортировал.
            var ordered = envelope.Result.Items
                .OrderByDescending(x => alreadyTracked.Contains(x.Id))
                .ToList();

            foreach (var item in ordered)
            {
                var row = new NearbyDeceasedRowViewModel(item);
                if (alreadyTracked.Contains(item.Id))
                    row.IsSelected = true;
                Results.Add(row);
            }
            HookRowSelectionEvents();

            HasSearched = true;
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(HasSummary));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(AddSelectedButtonText));
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
    private void OpenAppSettings() => geo.OpenAppSettings();

    /// <summary>
    /// Тап на найденную карточку → preview-экран (E17.1). Юзер сначала
    /// смотрит детали (даты, фото), потом сам решает подписываться или
    /// нет — защита от ошибочного тапа.
    /// </summary>
    [RelayCommand]
    private async Task OpenPreviewAsync(NearbyDeceasedRowViewModel? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"deceased-preview?deceasedId={item.Id}");
    }

    /// <summary>
    /// Batch-добавление выбранных умерших в Отслеживаемые. Каждый row
    /// дёргает отдельный POST (нет batch-эндпоинта на бэке); делаем
    /// параллельно через WhenAll. RelationshipType=Other, notify-флаги
    /// off, заметка пустая — позже юзер сможет дополнить в карточке.
    /// Идемпотентен: если кто-то уже отслеживается — backend перезаписывает
    /// в Active без ошибки.
    /// </summary>
    [RelayCommand]
    private async Task AddSelectedAsync()
    {
        var selected = Results.Where(x => x.IsSelected).ToList();
        if (selected.Count == 0) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            AddStatusMessage = null;

            var request = new AddMeTrackingRequest(
                RelationshipType: RelationshipTypes.Other,
                PersonalNotes: null,
                NotifyOnDeathAnniversary: false,
                NotifyOnBirthAnniversary: false);

            var tasks = selected.Select(row => trackedApi.TrackAsync(row.Id, request));
            var responses = await Task.WhenAll(tasks);

            var success = responses.Count(r => r.Result is not null);
            var failed = responses.Length - success;

            // Сбрасываем выделение успешно добавленных.
            for (int i = 0; i < responses.Length; i++)
                if (responses[i].Result is not null)
                    selected[i].IsSelected = false;

            AddStatusMessage = failed == 0
                ? $"Добавлено: {success}."
                : $"Добавлено: {success}, не удалось: {failed}.";
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
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");

    /// <summary>
    /// Тянем все отслеживаемые карточки (включая Archived/Muted), чтобы
    /// предотметить в Nearby. PageSize=200 — компромисс: у большинства
    /// юзеров их меньше, у активных трекеров часть не предотметится,
    /// но базовый UX работает.
    /// </summary>
    private async Task<HashSet<Guid>> LoadAlreadyTrackedIdsAsync()
    {
        try
        {
            var envelope = await trackedApi.GetListAsync(page: 1, pageSize: 200);
            if (envelope.Result is null) return new HashSet<Guid>();
            return envelope.Result.Items.Select(x => x.DeceasedId).ToHashSet();
        }
        catch
        {
            // Сетевая/auth ошибка — не валим основной флоу поиска, просто
            // не помечаем галки.
            return new HashSet<Guid>();
        }
    }
}

/// <summary>
/// Обёртка над NearbyDeceasedItem с pre-форматированными для XAML
/// строками (DistanceText, LifePeriod). IsSelected — для batch-добавления
/// нескольких выбранных в отслеживаемые без захода в карточку.
/// </summary>
public partial class NearbyDeceasedRowViewModel : ObservableObject
{
    public Guid Id { get; }
    public string FullName { get; }
    public string LifePeriod { get; }
    public string LocationText { get; }
    public string DistanceText { get; }
    public bool IsVerified { get; }

    [ObservableProperty]
    private bool _isSelected;

    public NearbyDeceasedRowViewModel(NearbyDeceasedItem source)
    {
        Id = source.Id;
        FullName = source.FullName;

        var birth = source.BirthDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "?";
        var death = source.DeathDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        LifePeriod = $"{birth} — {death}";

        var parts = new[] { source.City, source.CemeteryName, source.PlotNumber, source.GraveNumber }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        LocationText = string.Join(", ", parts);

        DistanceText = DistanceFormatter.Format(source.DistanceMeters);
        IsVerified = source.IsVerified;
    }
}
