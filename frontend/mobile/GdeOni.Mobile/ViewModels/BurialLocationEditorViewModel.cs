using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Geolocation;
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

            var envelope = await api.SetBurialLocationAsync(
                id, new SetBurialLocationRequest(lat, lon, accuracy));

            if (envelope.Result is null)
            {
                ErrorMessage = $"Не удалось сохранить: {envelope.ErrorCode} — {envelope.ErrorMessage}";
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
}
