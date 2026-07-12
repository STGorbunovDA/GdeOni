using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Geolocation;
using GdeOni.Mobile.Services.Network;
using GdeOni.Mobile.Shared.Notifications;
using GdeOni.Mobile.Shared.Utils;
using Refit;

namespace GdeOni.Mobile.ViewModels;

public partial class AtGraveViewModel(
    IDeceasedRecordsApi api,
    IGeolocationService geo,
    INetworkInfoService network,
    ILocalNotificationScheduler notificationScheduler) : ObservableObject
{
    [ObservableProperty]
    private bool _isVpnActive;

    // ----- Координаты -----
    // Source of truth — строковые поля для двунаправленного биндинга
    // с Entry'ями. Это позволяет вводить координаты вручную или
    // получать через GPS — оба пути пишут сюда. Парсятся
    // только на этапе Submit (см. TryParseLatitude / TryParseLongitude).
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

    public bool HasCoordinates =>
        CoordinateParser.TryParseLatitude(LatitudeInput, out _) &&
        CoordinateParser.TryParseLongitude(LongitudeInput, out _);

    public bool IsAccuracyLow =>
        CoordinateParser.TryParseAccuracy(AccuracyInput, out var acc) && acc is > 50;

    // ----- Форма -----
    [ObservableProperty] private string _firstName = "";
    [ObservableProperty] private string _lastName = "";
    [ObservableProperty] private string _middleName = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBirthDate))]
    private DateTime? _birthDate;
    [ObservableProperty] private DateTime _deathDate = DateTime.Today;
    [ObservableProperty] private string _shortDescription = "";
    [ObservableProperty] private string _biography = "";

    [ObservableProperty] private string _country = "Россия";
    [ObservableProperty] private string _city = "";
    [ObservableProperty] private string _cemeteryName = "";
    [ObservableProperty] private string _plotNumber = "";
    [ObservableProperty] private string _graveNumber = "";

    public IReadOnlyList<RelationshipOption> RelationshipOptions { get; } = RelationshipTypes.All;

    [ObservableProperty] private RelationshipOption _selectedRelationship = RelationshipTypes.All[6]; // Friend
    [ObservableProperty] private string _personalNotes = "";
    [ObservableProperty] private bool _notifyOnDeathAnniversary;
    [ObservableProperty] private bool _notifyOnBirthAnniversary;

    // E23. Без даты рождения тоггл бессмыслен (нечего планировать).
    // XAML скрывает Switch + Label по этому флагу, а OnBirthDateChanged
    // ниже сбрасывает _notifyOnBirthAnniversary в false, если юзер
    // включил тоггл, а потом убрал дату.
    public bool HasBirthDate => BirthDate.HasValue;

    partial void OnBirthDateChanged(DateTime? value)
    {
        if (!value.HasValue)
            NotifyOnBirthAnniversary = false;
    }

    // ----- Состояние -----
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

    [ObservableProperty] private bool _showOpenSettings;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasGeoError => !string.IsNullOrEmpty(GeoErrorMessage);

    /// <summary>
    /// Submit включается только когда координаты корректны (парсятся
    /// и попадают в допустимые диапазоны), базовая часть формы
    /// заполнена и нет активных сетевых операций.
    /// </summary>
    public bool CanSubmit =>
        HasCoordinates
        && !IsBusyGeo
        && !IsBusySubmit
        && !string.IsNullOrWhiteSpace(FirstName)
        && !string.IsNullOrWhiteSpace(LastName);

    partial void OnFirstNameChanged(string value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnLastNameChanged(string value) => OnPropertyChanged(nameof(CanSubmit));

    public void RefreshVpnStatus()
    {
        IsVpnActive = network.IsVpnActive();
    }

    [RelayCommand]
    public async Task RequestLocationAsync()
    {
        try
        {
            IsBusyGeo = true;
            GeoErrorMessage = null;
            ShowOpenSettings = false;

            RefreshVpnStatus();

            var result = await geo.RequestAndGetCurrentAsync();

            if (!result.Success)
            {
                GeoErrorMessage = result.ErrorMessage ?? "Не удалось получить координаты.";
                ShowOpenSettings = result.ErrorKind == GeolocationErrorKind.PermissionDeniedPermanent;
                return;
            }

            // GPS перезаписывает ручной ввод — пользователь явно нажал
            // "Получить координаты" и ожидает свежие данные.
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
        if (!CanSubmit)
            return;

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

            var request = new AddDeceasedAtGraveRequest(
                FirstName: FirstName.Trim(),
                LastName: LastName.Trim(),
                MiddleName: NullIfEmpty(MiddleName),
                BirthDate: BirthDate is null ? null : DateOnly.FromDateTime(BirthDate.Value),
                DeathDate: DateOnly.FromDateTime(DeathDate),
                ShortDescription: NullIfEmpty(ShortDescription),
                Biography: NullIfEmpty(Biography),
                GraveLocation: new AddDeceasedAtGraveLocationRequest(
                    Latitude: lat,
                    Longitude: lon,
                    AccuracyMeters: accuracy,
                    Country: NullIfEmpty(Country),
                    City: NullIfEmpty(City),
                    CemeteryName: NullIfEmpty(CemeteryName),
                    PlotNumber: NullIfEmpty(PlotNumber),
                    GraveNumber: NullIfEmpty(GraveNumber)),
                Tracking: new AddDeceasedAtGraveTrackingRequest(
                    RelationshipType: SelectedRelationship.Value,
                    PersonalNotes: NullIfEmpty(PersonalNotes),
                    NotifyOnDeathAnniversary: NotifyOnDeathAnniversary,
                    NotifyOnBirthAnniversary: NotifyOnBirthAnniversary));

            var envelope = await api.AddAtGraveAsync(request);

            if (envelope.Result is null)
            {
                ErrorMessage = $"Не удалось создать карточку: {envelope.ErrorCode} — {envelope.ErrorMessage}";
                return;
            }

            // E23. Если юзер включил хотя бы один тоггл — запрашиваем
            // permission и планируем уведомления. Сначала Birth (если
            // BirthDate задана и тоггл включён), затем Death (DeathDate
            // обязательна, тоггл проверяем). Failures глотаем — это
            // не блокер навигации на DeceasedDetailsPage.
            if (NotifyOnDeathAnniversary || (NotifyOnBirthAnniversary && BirthDate.HasValue))
            {
                try
                {
                    if (await notificationScheduler.EnsureNotificationPermissionAsync())
                    {
                        var fullName = $"{LastName.Trim()} {FirstName.Trim()}".Trim();
                        if (NotifyOnBirthAnniversary && BirthDate.HasValue)
                        {
                            await notificationScheduler.ScheduleAnniversaryAsync(
                                new AnniversaryReminder(
                                    envelope.Result.DeceasedId,
                                    fullName,
                                    DateOnly.FromDateTime(BirthDate.Value),
                                    AnniversaryKind.Birth));
                        }
                        if (NotifyOnDeathAnniversary)
                        {
                            await notificationScheduler.ScheduleAnniversaryAsync(
                                new AnniversaryReminder(
                                    envelope.Result.DeceasedId,
                                    fullName,
                                    DateOnly.FromDateTime(DeathDate),
                                    AnniversaryKind.Death));
                        }
                    }
                }
                catch
                {
                    // Notification scheduling — best-effort. Не валим
                    // основной flow добавления карточки из-за permission
                    // denied или платформенной ошибки.
                }
            }

            await Shell.Current.GoToAsync($"../deceased-details?deceasedId={envelope.Result.DeceasedId}");
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
            IsBusySubmit = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
