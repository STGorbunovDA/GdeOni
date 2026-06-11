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
/// E26. Редактирование карточки умершего. Любой трекающий + админ
/// (auth-check на бэке через ICanEditDeceasedPolicy — 403 если нет
/// права). Три секции: основное / метаданные / место захоронения,
/// каждая со своей кнопкой Сохранить. Раздельные PATCH'и:
///   - проще обработать частичные ошибки;
///   - в audit log одна запись = один Kind, читать админу понятнее.
/// </summary>
[QueryProperty(nameof(DeceasedId), "deceasedId")]
public partial class EditDeceasedViewModel(
    IDeceasedRecordsApi deceasedRecordsApi,
    ITrackedDeceasedApi trackedApi,
    IGeolocationService geolocation) : ObservableObject
{
    [ObservableProperty] private string _deceasedId = "";

    partial void OnDeceasedIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = LoadAsync();
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSavingMain;
    [ObservableProperty] private bool _isSavingMetadata;
    [ObservableProperty] private bool _isSavingLocation;
    [ObservableProperty] private bool _isBusyGeo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string? _statusMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public string PageTitle => string.IsNullOrEmpty(FullName) ? "Редактирование карточки" : $"Редактирование: {FullName}";
    public string FullName { get; private set; } = "";

    // ---- Основное ----
    [ObservableProperty] private string _firstName = "";
    [ObservableProperty] private string _lastName = "";
    [ObservableProperty] private string _middleName = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBirthDate))]
    private DateTime? _birthDate;

    [ObservableProperty] private DateTime _deathDate = DateTime.Today;
    [ObservableProperty] private string _shortDescription = "";
    [ObservableProperty] private string _biography = "";

    public bool HasBirthDate => BirthDate.HasValue;

    // ---- Метаданные ----
    [ObservableProperty] private string _epitaph = "";
    [ObservableProperty] private string _religion = "";
    [ObservableProperty] private string _source = "";
    [ObservableProperty] private bool _isMilitaryService;
    [ObservableProperty] private string _additionalInfo = "";

    // ---- Место захоронения ----
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCoordinates))]
    private string _latitudeInput = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCoordinates))]
    private string _longitudeInput = "";

    [ObservableProperty] private string _accuracyInput = "";
    [ObservableProperty] private string _country = "Россия";
    [ObservableProperty] private string _region = "";
    [ObservableProperty] private string _city = "";
    [ObservableProperty] private string _cemeteryName = "";
    [ObservableProperty] private string _plotNumber = "";
    [ObservableProperty] private string _graveNumber = "";

    public bool HasCoordinates =>
        CoordinateParser.TryParseLatitude(LatitudeInput, out _) &&
        CoordinateParser.TryParseLongitude(LongitudeInput, out _);

    private async Task LoadAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var id)) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var envelope = await trackedApi.GetDetailsAsync(id);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить карточку.";
                return;
            }

            var d = envelope.Result.Deceased;

            FullName = d.FullName;
            OnPropertyChanged(nameof(FullName));
            OnPropertyChanged(nameof(PageTitle));

            FirstName = d.FirstName;
            LastName = d.LastName;
            MiddleName = d.MiddleName ?? "";
            BirthDate = d.BirthDate?.ToDateTime(TimeOnly.MinValue);
            DeathDate = d.DeathDate.ToDateTime(TimeOnly.MinValue);
            ShortDescription = d.ShortDescription ?? "";
            Biography = d.Biography ?? "";

            Epitaph = d.Metadata.Epitaph ?? "";
            Religion = d.Metadata.Religion ?? "";
            Source = d.Metadata.Source ?? "";
            IsMilitaryService = d.Metadata.IsMilitaryService;
            AdditionalInfo = d.Metadata.AdditionalInfo ?? "";

            LatitudeInput = d.Latitude?.ToString("0.000000", CultureInfo.InvariantCulture) ?? "";
            LongitudeInput = d.Longitude?.ToString("0.000000", CultureInfo.InvariantCulture) ?? "";
            AccuracyInput = d.AccuracyMeters?.ToString("0", CultureInfo.InvariantCulture) ?? "";
            Country = d.Country ?? "";
            Region = d.Region ?? "";
            City = d.City ?? "";
            CemeteryName = d.CemeteryName ?? "";
            PlotNumber = d.PlotNumber ?? "";
            GraveNumber = d.GraveNumber ?? "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RequestGpsAsync()
    {
        if (!Guid.TryParse(DeceasedId, out _)) return;
        try
        {
            IsBusyGeo = true;
            var result = await geolocation.RequestAndGetCurrentAsync();
            if (!result.Success) return;
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

    [RelayCommand]
    private async Task SaveMainAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var id)) return;

        try
        {
            IsSavingMain = true;
            ErrorMessage = null;
            StatusMessage = null;

            var request = new UpdateMainInfoRequest(
                FirstName: FirstName.Trim(),
                LastName: LastName.Trim(),
                MiddleName: NullIfEmpty(MiddleName),
                BirthDate: BirthDate is null ? null : DateOnly.FromDateTime(BirthDate.Value),
                DeathDate: DateOnly.FromDateTime(DeathDate),
                ShortDescription: NullIfEmpty(ShortDescription),
                Biography: NullIfEmpty(Biography));

            var resp = await deceasedRecordsApi.UpdateMainInfoAsync(id, request);
            HandleResponse(resp, "Основная информация сохранена.");
        }
        catch (ApiException apiEx) { ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}"; }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsSavingMain = false; }
    }

    [RelayCommand]
    private async Task SaveMetadataAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var id)) return;

        try
        {
            IsSavingMetadata = true;
            ErrorMessage = null;
            StatusMessage = null;

            var request = new UpdateMetadataRequest(
                Epitaph: NullIfEmpty(Epitaph),
                Religion: NullIfEmpty(Religion),
                Source: NullIfEmpty(Source),
                IsMilitaryService: IsMilitaryService,
                AdditionalInfo: NullIfEmpty(AdditionalInfo));

            var resp = await deceasedRecordsApi.UpdateMetadataAsync(id, request);
            HandleResponse(resp, "Дополнительная информация сохранена.");
        }
        catch (ApiException apiEx) { ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}"; }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsSavingMetadata = false; }
    }

    [RelayCommand]
    private async Task SaveLocationAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var id)) return;

        double? lat = null, lon = null, acc = null;
        if (!string.IsNullOrWhiteSpace(LatitudeInput) && !string.IsNullOrWhiteSpace(LongitudeInput))
        {
            if (!CoordinateParser.TryParseLatitude(LatitudeInput, out var parsedLat))
            {
                ErrorMessage = "Широта должна быть числом в диапазоне [-90, 90].";
                return;
            }
            if (!CoordinateParser.TryParseLongitude(LongitudeInput, out var parsedLon))
            {
                ErrorMessage = "Долгота должна быть числом в диапазоне [-180, 180].";
                return;
            }
            lat = parsedLat;
            lon = parsedLon;
        }
        if (!string.IsNullOrWhiteSpace(AccuracyInput))
        {
            if (!CoordinateParser.TryParseAccuracy(AccuracyInput, out var parsedAcc))
            {
                ErrorMessage = "Точность должна быть неотрицательным числом (метры).";
                return;
            }
            acc = parsedAcc;
        }

        try
        {
            IsSavingLocation = true;
            ErrorMessage = null;
            StatusMessage = null;

            var request = new UpdateBurialLocationRequest(
                Latitude: lat,
                Longitude: lon,
                AccuracyMeters: acc,
                Country: NullIfEmpty(Country),
                Region: NullIfEmpty(Region),
                City: NullIfEmpty(City),
                CemeteryName: NullIfEmpty(CemeteryName),
                PlotNumber: NullIfEmpty(PlotNumber),
                GraveNumber: NullIfEmpty(GraveNumber));

            var resp = await deceasedRecordsApi.UpdateBurialLocationAsync(id, request);
            HandleResponse(resp, "Место захоронения сохранено.");
        }
        catch (ApiException apiEx) { ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}"; }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsSavingLocation = false; }
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");

    private void HandleResponse(HttpResponseMessage response, string successMessage)
    {
        if (response.IsSuccessStatusCode)
        {
            StatusMessage = successMessage;
            return;
        }

        var code = (int)response.StatusCode;
        ErrorMessage = code switch
        {
            403 => "Только отслеживающие карточку или админы могут её редактировать.",
            404 => "Карточка не найдена.",
            409 => "Похожая карточка уже существует. Проверьте имя и даты.",
            400 => "Введены некорректные данные.",
            _ => $"Ошибка сохранения (HTTP {code})."
        };
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
