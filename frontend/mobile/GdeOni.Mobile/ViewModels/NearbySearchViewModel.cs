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

            Results.Clear();
            foreach (var item in envelope.Result.Items)
                Results.Add(new NearbyDeceasedRowViewModel(item));

            HasSearched = true;
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(HasSummary));
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

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

/// <summary>
/// Обёртка над NearbyDeceasedItem с pre-форматированными для XAML
/// строками (DistanceText, LifePeriod).
/// </summary>
public sealed record NearbyDeceasedRowViewModel
{
    public Guid Id { get; }
    public string FullName { get; }
    public string LifePeriod { get; }
    public string LocationText { get; }
    public string DistanceText { get; }
    public bool IsVerified { get; }

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
