using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// E17.1: preview-страница карточки умершего. Открывается из поиска
/// (DeceasedSearchPage) при тапе на найденного. Здесь юзер ещё НЕ
/// подписан — он смотрит карточку и решает, "тот ли это умерший".
/// Кнопка внизу:
/// - "Открыть мою карточку" — если уже отслеживает (Active/Muted/Archived);
/// - "Добавить в отслеживание" — если ещё нет.
/// После подписки переходим на полную карточку (deceased-details).
/// </summary>
[QueryProperty(nameof(DeceasedId), "deceasedId")]
public partial class DeceasedPreviewViewModel(
    IDeceasedRecordsApi deceasedApi,
    ITrackedDeceasedApi trackingApi) : ObservableObject
{
    [ObservableProperty]
    private string _deceasedId = "";

    partial void OnDeceasedIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = LoadAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasData))]
    private DeceasedDetailsResponse? _data;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionButtonText))]
    private bool _isAlreadyTracked;

    [ObservableProperty]
    private string? _mainPhotoUrl;

    public bool HasData => Data is not null;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasMainPhoto => !string.IsNullOrEmpty(MainPhotoUrl);
    public bool HasNoMainPhoto => !HasMainPhoto;

    public string ActionButtonText => IsAlreadyTracked
        ? "Открыть мою карточку"
        : "Добавить в отслеживание";

    public string FullName => Data?.FullName ?? "—";

    public string LifePeriod
    {
        get
        {
            if (Data is null) return "—";
            var birth = Data.BirthDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "?";
            var death = Data.DeathDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            return $"{birth} — {death}";
        }
    }

    public string LocationText
    {
        get
        {
            if (Data is null) return "—";
            var parts = new[] { Data.Country, Data.City, Data.CemeteryName }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var joined = string.Join(", ", parts);
            return string.IsNullOrEmpty(joined) ? "место не указано" : joined;
        }
    }

    public bool HasBurialLocation => Data?.HasBurialLocation == true;
    public string Biography => Data?.Biography ?? "";
    public bool HasBiography => !string.IsNullOrWhiteSpace(Data?.Biography);
    public string ShortDescription => Data?.ShortDescription ?? "";
    public bool HasShortDescription => !string.IsNullOrWhiteSpace(Data?.ShortDescription);
    public bool IsVerified => Data?.IsVerified == true;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var id))
        {
            ErrorMessage = "Некорректный идентификатор.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            // 1. Загружаем саму карточку.
            var details = await deceasedApi.GetByIdAsync(id);
            if (details.Result is null)
            {
                ErrorMessage = details.ErrorMessage ?? "Карточка не найдена.";
                Data = null;
                return;
            }
            Data = details.Result;
            // Главное фото приходит прямо в ответе details (MainPhotoUrl),
            // без отдельного запроса за /media. Раньше тут был N+1: грузили
            // всю коллекцию media только ради url одного главного фото.
            MainPhotoUrl = details.Result.MainPhotoUrl;

            // 2. Проверяем, отслеживаем ли уже (от этого зависит текст
            // кнопки и поведение тапа).
            try
            {
                var exists = await trackingApi.IsTrackedAsync(id);
                IsAlreadyTracked = exists.Result?.Tracked == true;
            }
            catch
            {
                // Если exists упал — считаем что не отслеживается, чтобы
                // юзер мог нажать "Добавить". Backend всё равно идемпотентный.
                IsAlreadyTracked = false;
            }

            // Триггерим все computed.
            OnPropertyChanged(nameof(FullName));
            OnPropertyChanged(nameof(LifePeriod));
            OnPropertyChanged(nameof(LocationText));
            OnPropertyChanged(nameof(HasBurialLocation));
            OnPropertyChanged(nameof(Biography));
            OnPropertyChanged(nameof(HasBiography));
            OnPropertyChanged(nameof(ShortDescription));
            OnPropertyChanged(nameof(HasShortDescription));
            OnPropertyChanged(nameof(IsVerified));
            OnPropertyChanged(nameof(HasMainPhoto));
            OnPropertyChanged(nameof(HasNoMainPhoto));
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
    /// Главное действие. Если уже отслеживает — просто переход в полную
    /// карточку. Если нет — подписка (default Friend) и переход.
    /// </summary>
    [RelayCommand]
    private async Task PrimaryActionAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var id)) return;

        if (IsAlreadyTracked)
        {
            await Shell.Current.GoToAsync(
                $"//main/tracked/deceased-details?deceasedId={id}");
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var request = new AddMeTrackingRequest(
                RelationshipType: RelationshipTypes.Friend,
                PersonalNotes: null,
                NotifyOnDeathAnniversary: false,
                NotifyOnBirthAnniversary: false);

            var envelope = await trackingApi.TrackAsync(id, request);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось подписаться.";
                return;
            }

            await Shell.Current.GoToAsync(
                $"//main/tracked/deceased-details?deceasedId={id}");
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
    private async Task BackAsync()
    {
        // GoToAsync("..") в Shell иногда возвращается не туда, если
        // целевой route зарегистрирован глобально (deceased-preview
        // используется и из tracked-flow, и из админ-ленты правок).
        // Прямой PopAsync однозначно снимает верхний элемент стека и
        // возвращает на ту страницу с которой пришли.
        try
        {
            await Shell.Current.Navigation.PopAsync();
        }
        catch
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
