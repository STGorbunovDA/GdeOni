using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Services.Geolocation;
using GdeOni.Mobile.Shared.Notifications;
using Refit;

namespace GdeOni.Mobile.ViewModels;

[QueryProperty(nameof(DeceasedId), "deceasedId")]
public partial class DeceasedDetailsViewModel(
    ITrackedDeceasedApi api,
    IDeceasedMediaApi mediaApi,
    IDeceasedMemoriesApi memoriesApi,
    IDeceasedRecordsApi deceasedRecordsApi,
    IGeolocationService geolocationService,
    IAuthService authService,
    ILocalNotificationScheduler notificationScheduler) : ObservableObject
{
    // Кешируем текущий userId на время жизни VM. Используется для вычисления
    // CanEdit у воспоминаний (см. MemoryItemViewModel.From).
    private Guid? _currentUserId;

    // Видимость кнопки "Удалить" в шапке: только админам. Решение тут
    // (а не на бэке) — UX-обнаружение: незачем показывать кнопку, которая
    // у не-админа вернёт 403. Сам DELETE всё равно проверяет роль на бэке.
    [ObservableProperty]
    private bool _isCurrentUserAdmin;
    [ObservableProperty]
    private string _deceasedId = "";

    partial void OnDeceasedIdChanged(string value)
    {
        // QueryProperty проставляет id при пуше страницы — сразу подгружаем.
        if (Guid.TryParse(value, out _))
            _ = LoadAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasData))]
    private MyTrackedDeceasedDetailsResponse? _data;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasData => Data is not null;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsNotBusy => !IsBusy;

    // Удобные view-model геттеры — поля, которые нагляднее показывать
    // через computed строки, а не через биндинги конвертеров в XAML.
    public string FullName => Data?.Deceased.FullName ?? "—";

    public string LifePeriod
    {
        get
        {
            if (Data is null) return "—";
            var d = Data.Deceased;
            var birth = d.BirthDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "?";
            var death = d.DeathDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            return $"{birth} — {death}";
        }
    }

    public string Biography => Data?.Deceased.Biography ?? "";
    public bool HasBiography => !string.IsNullOrWhiteSpace(Data?.Deceased.Biography);

    public string ShortDescription => Data?.Deceased.ShortDescription ?? "";
    public bool HasShortDescription => !string.IsNullOrWhiteSpace(Data?.Deceased.ShortDescription);

    public bool HasBurialLocation => Data?.Deceased.HasBurialLocation == true;

    public string LocationText
    {
        get
        {
            if (Data is null) return "—";
            var d = Data.Deceased;
            var parts = new[] { d.Country, d.City, d.CemeteryName }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var joined = string.Join(", ", parts);
            return string.IsNullOrEmpty(joined) ? "место не указано" : joined;
        }
    }

    public string CoordinatesText
    {
        get
        {
            if (Data?.Deceased.Latitude is not double lat || Data.Deceased.Longitude is not double lon)
                return "координат нет";
            return $"{lat.ToString("0.000000", CultureInfo.InvariantCulture)}, " +
                   $"{lon.ToString("0.000000", CultureInfo.InvariantCulture)}";
        }
    }

    public string PlotInfo
    {
        get
        {
            if (Data is null) return "";
            var d = Data.Deceased;
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(d.PlotNumber)) parts.Add($"уч. {d.PlotNumber}");
            if (!string.IsNullOrWhiteSpace(d.GraveNumber)) parts.Add($"могила № {d.GraveNumber}");
            return string.Join(" · ", parts);
        }
    }

    public bool HasPlotInfo => !string.IsNullOrWhiteSpace(PlotInfo);

    public string RelationshipText => RelationshipTypes.Display(Data?.Tracking.RelationshipType);
    public string PersonalNotes => Data?.Tracking.PersonalNotes ?? "";
    public bool HasPersonalNotes => !string.IsNullOrWhiteSpace(Data?.Tracking.PersonalNotes);
    public bool NotifyOnDeathAnniversary => Data?.Tracking.NotifyOnDeathAnniversary == true;
    public bool NotifyOnBirthAnniversary => Data?.Tracking.NotifyOnBirthAnniversary == true;

    // E23 follow-up: edit-режим для tracking-настроек. Юзер открывает
    // блок read-only, нажимает "Изменить" → DraftXxx-поля загружаются из
    // текущего Data и редактируются → Save синхронизирует через PATCH
    // и пере-планирует уведомления по разнице старого/нового состояния.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsViewingTracking))]
    private bool _isEditingTracking;
    public bool IsViewingTracking => !IsEditingTracking;

    [ObservableProperty] private bool _draftNotifyOnDeath;
    [ObservableProperty] private bool _draftNotifyOnBirth;
    [ObservableProperty] private string _draftPersonalNotes = "";

    /// <summary>
    /// Нет смысла планировать "день рождения" без BirthDate — чекбокс
    /// disabled в UI, чтобы юзер не выставил флаг впустую.
    /// </summary>
    public bool CanNotifyOnBirth => Data?.Deceased.BirthDate is not null;

    public bool IsVerified => Data?.Deceased.IsVerified == true;

    public bool IsArchived => Data?.Tracking.Status == TrackStatuses.Archived;
    public bool IsActive => !IsArchived;

    public ObservableCollection<MemoryItemViewModel> Memories { get; } = new();
    public bool HasMemories => Memories.Count > 0;
    public bool HasNoMemories => Memories.Count == 0;

    public ObservableCollection<MediaListItem> DeceasedPhotos { get; } = new();
    public ObservableCollection<MediaListItem> GravePhotos { get; } = new();
    public ObservableCollection<MediaListItem> Documents { get; } = new();

    public bool HasDeceasedPhotos => DeceasedPhotos.Count > 0;
    public bool HasGravePhotos => GravePhotos.Count > 0;
    public bool HasDocuments => Documents.Count > 0;

    /// <summary>
    /// URL главного фото умершего (для hero-блока). Если main-photo не
    /// помечен или его нет — null, страница покажет 🕊-плейсхолдер.
    /// </summary>
    public string? MainPhotoUrl => DeceasedPhotos.FirstOrDefault(p => p.IsMainPhoto)?.Url
                                   ?? DeceasedPhotos.FirstOrDefault()?.Url;
    public bool HasMainPhoto => MainPhotoUrl is not null;
    public bool HasNoMainPhoto => MainPhotoUrl is null;

    [ObservableProperty]
    private bool _isUploading;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var id))
        {
            ErrorMessage = "Некорректный идентификатор карточки.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            // currentUserId нужен для вычисления CanEdit у воспоминаний.
            // Загружаем один раз — токен не меняется внутри жизни VM.
            if (_currentUserId is null)
            {
                var me = await authService.GetCurrentUserAsync();
                if (me is not null)
                {
                    _currentUserId = me.Id;
                    IsCurrentUserAdmin = string.Equals(me.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(me.Role, "Admin", StringComparison.OrdinalIgnoreCase);
                }
            }

            var envelope = await api.GetDetailsAsync(id);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Карточка не найдена.";
                Data = null;
                return;
            }

            Data = envelope.Result;

            var createdBy = envelope.Result.Deceased.CreatedByUserId;
            Memories.Clear();
            foreach (var m in envelope.Result.Deceased.Memories)
                Memories.Add(MemoryItemViewModel.From(m, createdBy, _currentUserId));

            // Триггерим перечитку всех computed свойств.
            OnPropertyChanged(nameof(FullName));
            OnPropertyChanged(nameof(LifePeriod));
            OnPropertyChanged(nameof(Biography));
            OnPropertyChanged(nameof(HasBiography));
            OnPropertyChanged(nameof(ShortDescription));
            OnPropertyChanged(nameof(HasShortDescription));
            OnPropertyChanged(nameof(HasBurialLocation));
            OnPropertyChanged(nameof(LocationText));
            OnPropertyChanged(nameof(CoordinatesText));
            OnPropertyChanged(nameof(PlotInfo));
            OnPropertyChanged(nameof(HasPlotInfo));
            OnPropertyChanged(nameof(RelationshipText));
            OnPropertyChanged(nameof(PersonalNotes));
            OnPropertyChanged(nameof(HasPersonalNotes));
            OnPropertyChanged(nameof(NotifyOnDeathAnniversary));
            OnPropertyChanged(nameof(NotifyOnBirthAnniversary));
            OnPropertyChanged(nameof(CanNotifyOnBirth));
            OnPropertyChanged(nameof(IsVerified));
            OnPropertyChanged(nameof(IsArchived));
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(HasMemories));
            OnPropertyChanged(nameof(HasNoMemories));

            await LoadMediaAsync(id);
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
    /// Перевод в архив через PATCH со Status=Archived. Карточка не удаляется,
    /// все остальные поля tracking-а (отношение, заметки, уведомления)
    /// сохраняются. Юзер сможет восстановить из вкладки "Архив".
    /// Физическое удаление (DELETE) только у админов — см. E13 в плане.
    /// </summary>
    [RelayCommand]
    private async Task ArchiveAsync()
    {
        await SetStatusAsync(TrackStatuses.Archived, navigateToArchive: true,
            confirmTitle: "Перенести в архив?",
            confirmMessage: $"{FullName} переместится в раздел «Архив». Вы сможете восстановить карточку оттуда в любой момент.",
            confirmAccept: "В архив");
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        await SetStatusAsync(TrackStatuses.Active, navigateToArchive: false,
            confirmTitle: null,
            confirmMessage: null,
            confirmAccept: null);
    }

    private async Task SetStatusAsync(
        string newStatus,
        bool navigateToArchive,
        string? confirmTitle,
        string? confirmMessage,
        string? confirmAccept)
    {
        if (!Guid.TryParse(DeceasedId, out var id) || Data is null)
            return;

        if (confirmTitle is not null)
        {
            var page = Shell.Current?.CurrentPage;
            if (page is null) return;
            var confirmed = await page.DisplayAlertAsync(
                confirmTitle, confirmMessage, confirmAccept, "Отмена");
            if (!confirmed) return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            // PATCH требует все tracking-поля сразу — берём из текущего Data,
            // меняем только Status.
            var t = Data.Tracking;
            var request = new UpdateTrackingRequest(
                RelationshipType: t.RelationshipType,
                PersonalNotes: t.PersonalNotes,
                NotifyOnDeathAnniversary: t.NotifyOnDeathAnniversary,
                NotifyOnBirthAnniversary: t.NotifyOnBirthAnniversary,
                TrackStatus: newStatus);

            var resp = await api.UpdateAsync(id, request);
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"Не удалось обновить статус (HTTP {(int)resp.StatusCode}).";
                return;
            }

            // Notifications: при архивации убираем все запланированные alarms
            // по карточке. При восстановлении — заново планируем (если тоггл
            // включён в tracking-настройках). Best-effort — failure не валит
            // флоу смены статуса.
            try
            {
                if (newStatus == TrackStatuses.Archived)
                {
                    await notificationScheduler.CancelAllForDeceasedAsync(id);
                }
                else if (newStatus == TrackStatuses.Active)
                {
                    await RescheduleAnniversariesAsync(id, t);
                }
            }
            catch { /* best-effort, см. AtGraveViewModel */ }

            // После архивации — на главный список (где архивных нет).
            // После восстановления — наоборот, на главный список (теперь там виден).
            await Shell.Current!.GoToAsync("//main/tracked");
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
    /// E23 follow-up. Переключение блока tracking в режим редактирования:
    /// копируем текущие значения в Draft-поля, чтобы Cancel мог их откатить
    /// без перезагрузки с бэка.
    /// </summary>
    [RelayCommand]
    private void StartEditTracking()
    {
        if (Data is null) return;
        DraftNotifyOnDeath = Data.Tracking.NotifyOnDeathAnniversary;
        DraftNotifyOnBirth = Data.Tracking.NotifyOnBirthAnniversary;
        DraftPersonalNotes = Data.Tracking.PersonalNotes ?? "";
        IsEditingTracking = true;
    }

    [RelayCommand]
    private void CancelEditTracking() => IsEditingTracking = false;

    /// <summary>
    /// PATCH /api/users/me/tracked-deceased/{id} с новыми значениями
    /// тогглов и заметки. После успеха — пере-планируем уведомления
    /// по диффу старого/нового состояния (см. ApplyNotificationDiffAsync).
    /// </summary>
    [RelayCommand]
    private async Task SaveTrackingAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var id) || Data is null) return;

        var t = Data.Tracking;
        // Запоминаем "до" для диффа уведомлений после успешного PATCH.
        var prevNotifyDeath = t.NotifyOnDeathAnniversary;
        var prevNotifyBirth = t.NotifyOnBirthAnniversary;

        // BirthDate отсутствует → флаг рождения принудительно false,
        // чтобы не упасть в schedule на пустую дату.
        var newNotifyBirth = CanNotifyOnBirth && DraftNotifyOnBirth;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var request = new UpdateTrackingRequest(
                RelationshipType: t.RelationshipType,
                PersonalNotes: string.IsNullOrWhiteSpace(DraftPersonalNotes) ? null : DraftPersonalNotes.Trim(),
                NotifyOnDeathAnniversary: DraftNotifyOnDeath,
                NotifyOnBirthAnniversary: newNotifyBirth,
                TrackStatus: t.Status);

            var resp = await api.UpdateAsync(id, request);
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"Не удалось сохранить (HTTP {(int)resp.StatusCode}).";
                return;
            }

            IsEditingTracking = false;

            // Сначала перезагружаем Data, потом дифф — иначе RescheduleAnniversariesAsync
            // прочитает старый Data.Tracking.
            await LoadAsync();

            try
            {
                await ApplyNotificationDiffAsync(
                    id,
                    prevNotifyDeath, DraftNotifyOnDeath,
                    prevNotifyBirth, newNotifyBirth);
            }
            catch { /* best-effort, см. AtGraveViewModel */ }
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
    /// E11. Берём текущие координаты пользователя через
    /// <see cref="IGeolocationService"/>, дёргаем backend
    /// (GET /tracked-deceased/{id}/route), получаем три deep-link'а,
    /// показываем ActionSheet и открываем выбранную карту во внешнем
    /// приложении через <see cref="Launcher"/>. Карта внутри приложения
    /// не строится — сторонние сервисы лучше знают пробки/маршрут.
    /// </summary>
    [RelayCommand]
    private async Task BuildRouteAsync()
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        if (!Guid.TryParse(DeceasedId, out var deceasedId) || Data is null)
            return;

        if (!HasBurialLocation)
        {
            await page.DisplayAlertAsync(
                "Координаты не указаны",
                "У этой могилы пока нет координат. Попросите автора карточки уточнить место захоронения.",
                "OK");
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var geo = await geolocationService.RequestAndGetCurrentAsync();
            if (!geo.Success || geo.Latitude is not double fromLat || geo.Longitude is not double fromLon)
            {
                await ShowGeolocationErrorAsync(page, geo);
                return;
            }

            var envelope = await api.GetRouteAsync(deceasedId, fromLat, fromLon, RoutingModes.Auto);
            if (envelope.Result is null)
            {
                // 409 BurialLocationNotSet — теоретически уже отсечён HasBurialLocation,
                // но карточка могла обновиться на backend между LoadAsync и тапом.
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось построить маршрут.";
                return;
            }

            await ShowRoutePickerAsync(page, envelope.Result);
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = apiEx.StatusCode == System.Net.HttpStatusCode.Conflict
                ? "У этой могилы пока нет координат. Попросите автора карточки уточнить место захоронения."
                : $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
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

    private async Task ShowGeolocationErrorAsync(Page page, GeolocationResult result)
    {
        var message = result.ErrorMessage ?? "Не удалось определить координаты.";

        if (result.ErrorKind == GeolocationErrorKind.PermissionDeniedPermanent)
        {
            var openSettings = await page.DisplayAlertAsync(
                "Нет доступа к геолокации",
                $"{message} Открыть настройки приложения?",
                "Открыть", "Отмена");
            if (openSettings)
                geolocationService.OpenAppSettings();
            return;
        }

        await page.DisplayAlertAsync("Геолокация", message, "OK");
    }

    private static async Task ShowRoutePickerAsync(Page page, RouteResponse route)
    {
        if (route.Links is null || route.Links.Count == 0)
        {
            await page.DisplayAlertAsync(
                "Маршрут",
                "Backend не вернул ссылок на карты. Попробуйте позже.",
                "OK");
            return;
        }

        // По решению 2026-05-13 на UI оставлен только Яндекс — открываем
        // его ссылку напрямую без ActionSheet. Backend по-прежнему отдаёт
        // три ссылки (yandex / google / 2gis), Google и 2GIS просто
        // игнорируем на стороне UI. Если в Links не оказалось yandex —
        // показываем понятное сообщение (теоретически такого быть не
        // должно: backend всегда отдаёт все три).
        var link = route.Links.FirstOrDefault(l =>
            string.Equals(l.Provider, RouteProviders.Yandex, StringComparison.OrdinalIgnoreCase));
        if (link is null)
        {
            await page.DisplayAlertAsync(
                "Маршрут",
                "Backend не вернул ссылку на Яндекс.Карты.",
                "OK");
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(new Uri(link.Url));
        }
        catch (Exception ex)
        {
            await page.DisplayAlertAsync(
                "Не удалось открыть карту",
                $"{ex.Message}\n\nПопробуйте открыть Яндекс.Карты в браузере.",
                "OK");
        }
    }

    [RelayCommand]
    private async Task UploadFileAsync()
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var choice = await page.DisplayActionSheetAsync(
            "Что загрузить?",
            "Отмена",
            null,
            "Фото умершего",
            "Фото могилы",
            "Документ");

        switch (choice)
        {
            case "Фото умершего":
                await UploadPhotoAsync(MediaKinds.DeceasedPhoto);
                break;
            case "Фото могилы":
                await UploadPhotoAsync(MediaKinds.GravePhoto);
                break;
            case "Документ":
                await UploadDocumentAsync();
                break;
        }
    }

    /// <summary>
    /// Тап по фото в галерее — показывает ActionSheet с действиями:
    /// сделать главным (только если ещё не main), удалить.
    /// </summary>
    [RelayCommand]
    private async Task PhotoTappedAsync(MediaListItem? item)
    {
        if (item is null || !Guid.TryParse(DeceasedId, out var deceasedId)) return;

        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var actions = new List<string>();
        // "Сделать главным" доступна только для DeceasedPhoto и только
        // если ещё не main — на GravePhoto/Document main-photo нет смысла.
        var canBeMain = item.Kind == MediaKinds.DeceasedPhotoString && !item.IsMainPhoto;
        if (canBeMain) actions.Add("Сделать главным");
        actions.Add("Удалить");

        var choice = await page.DisplayActionSheetAsync(
            item.OriginalFileName,
            "Отмена",
            null,
            actions.ToArray());

        switch (choice)
        {
            case "Сделать главным":
                await SetMainPhotoAsync(deceasedId, item.Id);
                break;
            case "Удалить":
                await DeleteMediaAsync(item);
                break;
        }
    }

    private async Task SetMainPhotoAsync(Guid deceasedId, Guid mediaId)
    {
        try
        {
            IsUploading = true;
            var resp = await mediaApi.SetMainPhotoAsync(deceasedId, mediaId);
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"Не удалось сделать главным (HTTP {(int)resp.StatusCode}).";
                return;
            }
            await LoadMediaAsync(deceasedId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsUploading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteMediaAsync(MediaListItem? item)
    {
        if (item is null || !Guid.TryParse(DeceasedId, out var deceasedId))
            return;

        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var confirmed = await page.DisplayAlertAsync(
            "Удалить файл?",
            $"{item.OriginalFileName} будет удалён без возможности восстановления.",
            "Удалить",
            "Отмена");
        if (!confirmed) return;

        try
        {
            IsUploading = true;
            var resp = await mediaApi.DeleteAsync(deceasedId, item.Id);
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"Не удалось удалить (HTTP {(int)resp.StatusCode}).";
                return;
            }
            await LoadMediaAsync(deceasedId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsUploading = false;
        }
    }

    private async Task UploadPhotoAsync(int kind)
    {
        if (!Guid.TryParse(DeceasedId, out var deceasedId)) return;

        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync();
            var photo = photos?.FirstOrDefault();
            if (photo is null) return;

            await using var stream = await photo.OpenReadAsync();
            await UploadStreamAsync(deceasedId, stream, photo.FileName, photo.ContentType, kind);
        }
        catch (PermissionException)
        {
            ErrorMessage = "Нет разрешения на доступ к фото. Откройте настройки приложения.";
        }
        catch (FeatureNotSupportedException)
        {
            ErrorMessage = "Выбор фото не поддерживается на устройстве.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка выбора фото: {ex.Message}";
        }
    }

    private async Task UploadDocumentAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var deceasedId)) return;

        try
        {
            var doc = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите документ"
            });
            if (doc is null) return;

            await using var stream = await doc.OpenReadAsync();
            await UploadStreamAsync(deceasedId, stream, doc.FileName, doc.ContentType, MediaKinds.Document);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка выбора документа: {ex.Message}";
        }
    }

    private async Task UploadStreamAsync(
        Guid deceasedId,
        Stream content,
        string fileName,
        string contentType,
        int kind)
    {
        try
        {
            IsUploading = true;
            ErrorMessage = null;

            var part = new StreamPart(content, fileName, contentType);
            var envelope = await mediaApi.UploadAsync(deceasedId, part, kind, description: null);

            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Загрузка не удалась.";
                return;
            }

            await LoadMediaAsync(deceasedId);

            // Auto-promote: если только что загрузили фото умершего и до этого
            // ни одно фото не было main, делаем новое — главным. Иначе аватар
            // на TrackedListPage останется пустым (backend подставляет туда
            // только MainPhotoUrl).
            if (kind == MediaKinds.DeceasedPhoto
                && DeceasedPhotos.Count > 0
                && DeceasedPhotos.All(p => !p.IsMainPhoto))
            {
                try
                {
                    await mediaApi.SetMainPhotoAsync(deceasedId, envelope.Result.MediaId);
                    await LoadMediaAsync(deceasedId);
                }
                catch
                {
                    // Назначение main не критично — оставим как обычное фото.
                    // Юзер позже сможет сделать вручную (когда добавим кнопку).
                }
            }
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
            ErrorMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsUploading = false;
        }
    }

    private async Task LoadMediaAsync(Guid deceasedId)
    {
        try
        {
            var envelope = await mediaApi.GetListAsync(deceasedId, page: 1, pageSize: 100);
            if (envelope.Result is null)
                return;

            DeceasedPhotos.Clear();
            GravePhotos.Clear();
            Documents.Clear();

            foreach (var item in envelope.Result.Items)
            {
                switch (item.Kind)
                {
                    case MediaKinds.DeceasedPhotoString:
                        DeceasedPhotos.Add(item);
                        break;
                    case MediaKinds.GravePhotoString:
                        GravePhotos.Add(item);
                        break;
                    case MediaKinds.DocumentString:
                        Documents.Add(item);
                        break;
                }
            }

            OnPropertyChanged(nameof(HasDeceasedPhotos));
            OnPropertyChanged(nameof(HasGravePhotos));
            OnPropertyChanged(nameof(HasDocuments));
            OnPropertyChanged(nameof(MainPhotoUrl));
            OnPropertyChanged(nameof(HasMainPhoto));
            OnPropertyChanged(nameof(HasNoMainPhoto));
        }
        catch
        {
            // Если медиа не загрузились — не блокируем остальную карточку,
            // юзер видит детали с пустыми секциями. Ошибка отображается
            // только если она была на upload/delete.
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("//main/tracked");
    }

    /// <summary>
    /// E26. Открыть EditDeceasedPage для редактирования карточки.
    /// Видимость кнопки в UI не ограничена — 403 (если юзер не трекает
    /// и не админ) показывается уже на странице редактирования.
    /// </summary>
    [RelayCommand]
    private async Task EditDeceasedAsync()
    {
        if (!Guid.TryParse(DeceasedId, out _)) return;
        await Shell.Current.GoToAsync($"edit-deceased?deceasedId={DeceasedId}");
    }

    /// <summary>
    /// F17.9 mobile. История правок этой карточки. Кнопка скрыта для не-админов
    /// через IsCurrentUserAdmin; backend дублирует проверку и вернёт 403.
    /// </summary>
    [RelayCommand]
    private async Task OpenEditsHistoryAsync()
    {
        if (!Guid.TryParse(DeceasedId, out _)) return;
        await Shell.Current.GoToAsync($"edits-history?deceasedId={DeceasedId}");
    }

    /// <summary>
    /// Жёсткое удаление карточки (DELETE /api/deceased-records/{id}).
    /// Доступно только админу — кнопка скрыта для остальных через
    /// <see cref="IsCurrentUserAdmin"/>, бэк независимо вернёт 403 если
    /// что-то пойдёт не так. После удаления отменяем annivers-alarms
    /// (best-effort) и возвращаемся на главный список.
    /// </summary>
    [RelayCommand]
    private async Task DeleteDeceasedAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var id) || Data is null) return;

        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var confirmed = await page.DisplayAlertAsync(
            "Удалить карточку?",
            $"Карточка {FullName} будет удалена полностью, включая фото, документы и воспоминания. Действие необратимо.",
            "Удалить",
            "Отмена");
        if (!confirmed) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var resp = await deceasedRecordsApi.DeleteAsync(id);
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = (int)resp.StatusCode switch
                {
                    403 => "Удалять карточки могут только администраторы.",
                    404 => "Карточка уже удалена.",
                    _ => $"Не удалось удалить (HTTP {(int)resp.StatusCode})."
                };
                return;
            }

            try { await notificationScheduler.CancelAllForDeceasedAsync(id); }
            catch { /* best-effort */ }

            await Shell.Current!.GoToAsync("//main/tracked");
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
    /// E20. Открыть редактор координат места захоронения с пред-заполнением
    /// текущих lat/lon/accuracy. Backend сохранит адресные поля при сохранении.
    /// Доступ ограничен 403'ом на backend (автор или админ).
    /// </summary>
    [RelayCommand]
    private async Task EditCoordinatesAsync()
    {
        if (!Guid.TryParse(DeceasedId, out _) || Data is null)
            return;

        var d = Data.Deceased;
        var lat = d.Latitude?.ToString("0.000000", CultureInfo.InvariantCulture) ?? "";
        var lon = d.Longitude?.ToString("0.000000", CultureInfo.InvariantCulture) ?? "";
        var acc = d.AccuracyMeters?.ToString("0", CultureInfo.InvariantCulture) ?? "";

        await Shell.Current.GoToAsync(
            $"burial-location-editor?deceasedId={DeceasedId}&lat={lat}&lon={lon}&acc={acc}");
    }

    // ==================== E12. Воспоминания ====================

    /// <summary>
    /// Открывает MemoryEditorPage в Create-режиме (memoryId не передаём).
    /// </summary>
    [RelayCommand]
    private async Task AddMemoryAsync()
    {
        if (!Guid.TryParse(DeceasedId, out _)) return;
        await Shell.Current.GoToAsync($"memory-editor?deceasedId={DeceasedId}");
    }

    /// <summary>
    /// Открывает MemoryEditorPage в Edit-режиме: передаём memoryId и
    /// текущий текст (url-encoded). VM в OnAppearing проставит Text.
    /// </summary>
    [RelayCommand]
    private async Task EditMemoryAsync(MemoryItemViewModel? memory)
    {
        if (memory is null || !memory.CanEdit) return;
        if (!Guid.TryParse(DeceasedId, out _)) return;

        var encoded = Uri.EscapeDataString(memory.Text);
        await Shell.Current.GoToAsync(
            $"memory-editor?deceasedId={DeceasedId}&memoryId={memory.Id}&text={encoded}");
    }

    [RelayCommand]
    private async Task DeleteMemoryAsync(MemoryItemViewModel? memory)
    {
        if (memory is null || !memory.CanEdit) return;
        if (!Guid.TryParse(DeceasedId, out var deceasedId)) return;

        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var confirmed = await page.DisplayAlertAsync(
            "Удалить воспоминание?",
            "Восстановить будет нельзя.",
            "Удалить",
            "Отмена");
        if (!confirmed) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var resp = await memoriesApi.DeleteAsync(deceasedId, memory.Id);
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"Не удалось удалить (HTTP {(int)resp.StatusCode}).";
                return;
            }

            await LoadAsync();
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
    /// E23 follow-up. Применяет дифф уведомлений после редактирования
    /// тогглов в edit-режиме: новые включения → schedule (после permission),
    /// выключения → cancel конкретного kind. Чтобы не перепланировать заново
    /// то, что уже было запланировано.
    /// </summary>
    private async Task ApplyNotificationDiffAsync(
        Guid deceasedId,
        bool prevDeath, bool newDeath,
        bool prevBirth, bool newBirth)
    {
        if (Data is null) return;

        var deathChanged = prevDeath != newDeath;
        var birthChanged = prevBirth != newBirth;
        if (!deathChanged && !birthChanged) return;

        // Permission запрашиваем только если есть что планировать заново.
        var needSchedule = (newDeath && !prevDeath) || (newBirth && !prevBirth);
        if (needSchedule && !await notificationScheduler.EnsureNotificationPermissionAsync())
        {
            // Permission отказан — не планируем. Cancel'ы для выключенных
            // тогглов всё равно делаем (на случай если они уже были в системе).
        }

        var fullName = Data.Deceased.FullName;

        if (birthChanged)
        {
            if (newBirth && Data.Deceased.BirthDate is DateOnly birth)
            {
                await notificationScheduler.ScheduleAnniversaryAsync(
                    new AnniversaryReminder(deceasedId, fullName, birth, AnniversaryKind.Birth));
            }
            else
            {
                await notificationScheduler.CancelAsync(deceasedId, AnniversaryKind.Birth);
            }
        }

        if (deathChanged)
        {
            if (newDeath)
            {
                await notificationScheduler.ScheduleAnniversaryAsync(
                    new AnniversaryReminder(deceasedId, fullName, Data.Deceased.DeathDate, AnniversaryKind.Death));
            }
            else
            {
                await notificationScheduler.CancelAsync(deceasedId, AnniversaryKind.Death);
            }
        }
    }

    /// <summary>
    /// Восстановление annivers-alarms по карточке: при restore из архива
    /// заново планируем уведомления для тех тогглов, что были включены.
    /// Permission запрашиваем только если есть что планировать.
    /// </summary>
    private async Task RescheduleAnniversariesAsync(Guid deceasedId, MyTrackingInfoResponse tracking)
    {
        if (Data is null) return;
        if (!tracking.NotifyOnDeathAnniversary &&
            !(tracking.NotifyOnBirthAnniversary && Data.Deceased.BirthDate.HasValue))
            return;

        if (!await notificationScheduler.EnsureNotificationPermissionAsync())
            return;

        var fullName = Data.Deceased.FullName;
        if (tracking.NotifyOnBirthAnniversary && Data.Deceased.BirthDate is DateOnly birth)
        {
            await notificationScheduler.ScheduleAnniversaryAsync(
                new AnniversaryReminder(deceasedId, fullName, birth, AnniversaryKind.Birth));
        }
        if (tracking.NotifyOnDeathAnniversary)
        {
            await notificationScheduler.ScheduleAnniversaryAsync(
                new AnniversaryReminder(deceasedId, fullName, Data.Deceased.DeathDate, AnniversaryKind.Death));
        }
    }
}
