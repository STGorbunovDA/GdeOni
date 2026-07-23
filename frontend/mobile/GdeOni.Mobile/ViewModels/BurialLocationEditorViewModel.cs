using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Geolocation;
using GdeOni.Mobile.Shared.Geo;
using GdeOni.Mobile.Shared.Utils;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// E20. Редактор координат места захоронения для существующей карточки.
/// Дёргает PUT /api/deceased-records/{id}/burial-location/from-gps —
/// backend сохраняет адресные поля и обновляет только lat/lon/accuracy.
/// </summary>
[QueryProperty(nameof(DeceasedId), "deceasedId")]
[QueryProperty(nameof(InitialLatitude), "lat")]
[QueryProperty(nameof(InitialLongitude), "lon")]
[QueryProperty(nameof(InitialAccuracy), "acc")]
public partial class BurialLocationEditorViewModel(
    IDeceasedRecordsApi api,
    IGeoApi geoApi,
    IGeolocationService geo) : ObservableObject
{
    [ObservableProperty]
    private string _deceasedId = "";

    /// <summary>Текущие значения с DeceasedDetailsPage — приходят как строки
    /// через QueryProperty (Shell route params). VM сам форматирует в нужный
    /// вид при первом проставлении. Если у карточки координат ещё нет —
    /// поля останутся пустыми.</summary>
    public string InitialLatitude
    {
        set => LatitudeInput = value ?? "";
    }
    public string InitialLongitude
    {
        set => LongitudeInput = value ?? "";
    }
    public string InitialAccuracy
    {
        set => AccuracyInput = value ?? "";
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCoordinates))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private string _latitudeInput = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCoordinates))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private string _longitudeInput = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAccuracyLow))]
    private string _accuracyInput = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private bool _isBusyGeo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private bool _isBusySubmit;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGeoError))]
    private string? _geoErrorMessage;

    [ObservableProperty]
    private bool _showOpenSettings;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasGeoError => !string.IsNullOrEmpty(GeoErrorMessage);

    public bool HasCoordinates =>
        CoordinateParser.TryParseLatitude(LatitudeInput, out _) &&
        CoordinateParser.TryParseLongitude(LongitudeInput, out _);

    public bool IsAccuracyLow =>
        CoordinateParser.TryParseAccuracy(AccuracyInput, out var acc) && acc is > 50;

    public bool CanSubmit => HasCoordinates && !IsBusyGeo && !IsBusySubmit;

    // ----- D41. Адрес: подставляется по координатам, правится руками -----
    [ObservableProperty] private string _country = "";
    [ObservableProperty] private string _city = "";
    [ObservableProperty] private bool _isResolvingAddress;

    /// <summary>Что подставили в прошлый раз — чтобы не затирать ручные правки.</summary>
    private string _autoCountry = "";
    private string _autoCity = "";

    /// <summary>
    /// Поля карточки, которых нет на этом экране. Держим их, чтобы PATCH
    /// burial-location не обнулил регион, кладбище, участок и номер могилы.
    /// </summary>
    private string? _region;
    private string? _cemeteryName;
    private string? _plotNumber;
    private string? _graveNumber;

    /// <summary>Координаты, с которыми экран открылся: по ним видно, двигал ли юзер точку.</summary>
    private string _initialLat = "";
    private string _initialLon = "";
    private bool _cardLoaded;

    private CancellationTokenSource? _addressCts;

    /// <summary>
    /// Подтягивает адрес карточки. Нужен по двум причинам: показать текущие
    /// страну/город в полях и сохранить нетронутыми те поля, которых на этом
    /// экране нет (PATCH шлёт объект целиком и обнулил бы их).
    /// </summary>
    public async Task LoadCardAsync()
    {
        if (_cardLoaded) return;
        if (!Guid.TryParse(DeceasedId, out var id)) return;

        try
        {
            var envelope = await api.GetByIdAsync(id);
            if (envelope.Result is not { } d) return;

            Country = d.Country ?? "";
            City = d.City ?? "";

            // Адрес карточки считаем «нашей» подстановкой: при сдвиге точки
            // его можно перезаписать. Ручные правки юзера появятся позже и
            // перестанут совпадать с _auto* — тогда мы их не тронем.
            _autoCountry = d.Country ?? "";
            _autoCity = d.City ?? "";

            _region = d.Region;
            _cemeteryName = d.CemeteryName;
            _plotNumber = d.PlotNumber;
            _graveNumber = d.GraveNumber;

            _initialLat = LatitudeInput;
            _initialLon = LongitudeInput;
            _cardLoaded = true;

            // Город пуст — заполнить его нечем, кроме координат.
            if (string.IsNullOrWhiteSpace(City))
                ScheduleAddressResolve();
        }
        catch
        {
            // Не смогли подтянуть карточку — экран всё равно должен работать:
            // координаты сохранятся, адрес юзер при желании впишет сам.
        }
    }

    // Ловим все способы задать координаты разом: GPS, тап по карте, ручной ввод.
    partial void OnLatitudeInputChanged(string value) => OnCoordinatesChanged();
    partial void OnLongitudeInputChanged(string value) => OnCoordinatesChanged();

    private void OnCoordinatesChanged()
    {
        // До загрузки карточки не дёргаемся: иначе перетрём город, хотя юзер
        // ещё ничего не двигал (координаты только что пришли из карточки).
        if (!_cardLoaded) return;

        var coordsMoved =
            !string.Equals(LatitudeInput, _initialLat, StringComparison.Ordinal)
            || !string.Equals(LongitudeInput, _initialLon, StringComparison.Ordinal);

        // Обновляем адрес, если точку сдвинули ИЛИ города всё ещё нет.
        if (coordsMoved || string.IsNullOrWhiteSpace(City))
            ScheduleAddressResolve();
    }

    /// <summary>
    /// Debounce нужен из-за ручного ввода координат: без него запрос уходил
    /// бы на каждый набранный символ.
    /// </summary>
    private void ScheduleAddressResolve()
    {
        if (!CoordinateParser.TryParseLatitude(LatitudeInput, out var lat)) return;
        if (!CoordinateParser.TryParseLongitude(LongitudeInput, out var lon)) return;

        _addressCts?.Cancel();
        _addressCts = new CancellationTokenSource();
        var token = _addressCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(700, token);
                await ResolveAddressAsync(lat, lon, token);
            }
            catch (OperationCanceledException)
            {
                // Координаты снова изменились — этот запрос уже неактуален.
            }
        }, token);
    }

    private async Task ResolveAddressAsync(double lat, double lon, CancellationToken token)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() => IsResolvingAddress = true);

            var envelope = await geoApi.ReverseAsync(lat, lon, token);
            var address = envelope.Result;
            if (address is null || token.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Country = AddressAutofill.Merge(Country, _autoCountry, address.Country);
                City = AddressAutofill.Merge(City, _autoCity, address.City);
                _autoCountry = address.Country ?? "";
                _autoCity = address.City ?? "";
            });
        }
        catch (Exception)
        {
            // Адреса по точке нет или геокодер недоступен — поля остаются
            // под ручной ввод, сценарий правки координат не ломается.
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() => IsResolvingAddress = false);
        }
    }

    [RelayCommand]
    public async Task RequestLocationAsync()
    {
        try
        {
            IsBusyGeo = true;
            GeoErrorMessage = null;
            ShowOpenSettings = false;

            var result = await geo.RequestAndGetCurrentAsync();
            if (!result.Success)
            {
                GeoErrorMessage = result.ErrorMessage ?? "Не удалось получить координаты.";
                ShowOpenSettings = result.ErrorKind == GeolocationErrorKind.PermissionDeniedPermanent;
                return;
            }

            if (result.Latitude is double lat)
                LatitudeInput = lat.ToString("0.000000", CultureInfo.InvariantCulture);
            if (result.Longitude is double lon)
                LongitudeInput = lon.ToString("0.000000", CultureInfo.InvariantCulture);
            AccuracyInput = result.AccuracyMeters is double acc
                ? acc.ToString("0", CultureInfo.InvariantCulture)
                : "";
        }
        finally
        {
            IsBusyGeo = false;
        }
    }

    /// <summary>Точка выбрана тапом по карте — ручная точка, GPS-точности нет.</summary>
    public void ApplyPickedLocation(double latitude, double longitude)
    {
        LatitudeInput = latitude.ToString("0.000000", CultureInfo.InvariantCulture);
        LongitudeInput = longitude.ToString("0.000000", CultureInfo.InvariantCulture);
        AccuracyInput = "";
    }

    [RelayCommand]
    private void OpenAppSettings() => geo.OpenAppSettings();

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit) return;
        if (!Guid.TryParse(DeceasedId, out var id))
        {
            ErrorMessage = "Некорректный идентификатор карточки.";
            return;
        }

        if (!CoordinateParser.TryParseLatitude(LatitudeInput, out var lat))
        {
            ErrorMessage = "Широта должна быть числом в диапазоне [-90, 90].";
            return;
        }
        if (!CoordinateParser.TryParseLongitude(LongitudeInput, out var lon))
        {
            ErrorMessage = "Долгота должна быть числом в диапазоне [-180, 180].";
            return;
        }

        double? accuracy = null;
        if (!string.IsNullOrWhiteSpace(AccuracyInput))
        {
            if (!CoordinateParser.TryParseAccuracy(AccuracyInput, out var acc))
            {
                ErrorMessage = "Точность должна быть неотрицательным числом (метры).";
                return;
            }
            accuracy = acc;
        }

        try
        {
            IsBusySubmit = true;
            ErrorMessage = null;

            // D41. Раньше слали from-gps — он сохраняет только координаты и
            // намеренно не трогает адрес. Теперь адрес правится здесь же,
            // поэтому шлём PATCH целиком. Регион, кладбище, участок и номер
            // могилы передаём как есть из карточки — иначе PATCH их обнулит.
            var response = await api.UpdateBurialLocationAsync(
                id,
                new UpdateBurialLocationRequest(
                    Latitude: lat,
                    Longitude: lon,
                    AccuracyMeters: accuracy,
                    Country: NullIfEmpty(Country),
                    Region: _region,
                    City: NullIfEmpty(City),
                    CemeteryName: _cemeteryName,
                    PlotNumber: _plotNumber,
                    GraveNumber: _graveNumber));

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = $"Не удалось сохранить: HTTP {(int)response.StatusCode}";
                return;
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = apiEx.StatusCode switch
            {
                System.Net.HttpStatusCode.Forbidden =>
                    "Координаты можно править только автору карточки или администратору.",
                _ => $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}"
            };
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
            IsBusySubmit = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");

    /// <summary>Пустая строка → null: бэк отличает «не указано» от «пусто».</summary>
    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
