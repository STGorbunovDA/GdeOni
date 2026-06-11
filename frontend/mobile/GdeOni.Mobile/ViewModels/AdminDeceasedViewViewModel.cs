using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// D27. Админский просмотр карточки умершего без её добавления в
/// отслеживание. Используется когда админ нашёл карточку через
/// AdminFindDeceasedPage и хочет управлять её содержимым (загрузить
/// фото/документы, удалить медиа, поменять главное фото).
///
/// <para>
/// В отличие от <see cref="DeceasedDetailsViewModel"/> не дёргает
/// my-tracked-deceased endpoint — там бы 404, потому что админ эту
/// карточку не трекает. Использует публичный GET /api/deceased-records/{id}.
/// </para>
/// </summary>
[QueryProperty(nameof(DeceasedId), "deceasedId")]
public partial class AdminDeceasedViewViewModel(
    IDeceasedRecordsApi deceasedApi,
    IDeceasedMediaApi mediaApi,
    IDeceasedMemoriesApi memoriesApi) : ObservableObject
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
    [NotifyPropertyChangedFor(nameof(MainPhotoUrl))]
    [NotifyPropertyChangedFor(nameof(HasMainPhoto))]
    [NotifyPropertyChangedFor(nameof(HasNoMainPhoto))]
    [NotifyPropertyChangedFor(nameof(FullName))]
    [NotifyPropertyChangedFor(nameof(LifePeriod))]
    [NotifyPropertyChangedFor(nameof(Biography))]
    [NotifyPropertyChangedFor(nameof(HasBiography))]
    [NotifyPropertyChangedFor(nameof(ShortDescription))]
    [NotifyPropertyChangedFor(nameof(HasShortDescription))]
    [NotifyPropertyChangedFor(nameof(LocationText))]
    [NotifyPropertyChangedFor(nameof(HasBurialLocation))]
    [NotifyPropertyChangedFor(nameof(CoordinatesText))]
    [NotifyPropertyChangedFor(nameof(IsVerified))]
    [NotifyPropertyChangedFor(nameof(VerifyButtonText))]
    [NotifyPropertyChangedFor(nameof(CreatedAtDisplay))]
    private DeceasedDetailsResponse? _data;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isUploading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasData => Data is not null;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsNotBusy => !IsBusy;

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

    public string Biography => Data?.Biography ?? "";
    public bool HasBiography => !string.IsNullOrWhiteSpace(Data?.Biography);
    public string ShortDescription => Data?.ShortDescription ?? "";
    public bool HasShortDescription => !string.IsNullOrWhiteSpace(Data?.ShortDescription);

    public bool HasBurialLocation => Data?.HasBurialLocation == true;

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

    public string CoordinatesText
    {
        get
        {
            if (Data?.Latitude is not double lat || Data.Longitude is not double lon)
                return "координат нет";
            return $"{lat.ToString("0.000000", CultureInfo.InvariantCulture)}, " +
                   $"{lon.ToString("0.000000", CultureInfo.InvariantCulture)}";
        }
    }

    public bool IsVerified => Data?.IsVerified == true;

    public string CreatedAtDisplay => Data?.CreatedAtUtc.ToLocalTime()
        .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) ?? "";

    // Медиа-коллекции.
    public ObservableCollection<MediaListItem> DeceasedPhotos { get; } = new();
    public ObservableCollection<MediaListItem> GravePhotos { get; } = new();
    public ObservableCollection<MediaListItem> Documents { get; } = new();

    // D30. Воспоминания. Видны админу для модерации (увидеть жалобы,
    // мат, спам, удалить токсичный текст) без необходимости лезть в
    // обычную карточку юзера. Здесь храним DTO как есть — поля
    // ModerationStatus и AuthorUserId полезны при принятии решения.
    public ObservableCollection<DeceasedMemoryResponse> Memories { get; } = new();
    // Counters — обычные ObservableProperty: XAML binding получает
    // изменения автоматически. Геттеры HasX/HasNoX от counter'ов
    // через [NotifyPropertyChangedFor] перерасчёт триггерится сам.
    // Без этого XAML на первом рендере "застревает" в false и
    // "Нет фото." не показывается, а пустой CollectionView занимает
    // место без плиток — ровно ваш симптом.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDeceasedPhotos))]
    [NotifyPropertyChangedFor(nameof(HasNoDeceasedPhotos))]
    [NotifyPropertyChangedFor(nameof(MainPhotoUrl))]
    [NotifyPropertyChangedFor(nameof(HasMainPhoto))]
    [NotifyPropertyChangedFor(nameof(HasNoMainPhoto))]
    private int _deceasedPhotosCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGravePhotos))]
    [NotifyPropertyChangedFor(nameof(HasNoGravePhotos))]
    private int _gravePhotosCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDocuments))]
    [NotifyPropertyChangedFor(nameof(HasNoDocuments))]
    private int _documentsCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMemories))]
    [NotifyPropertyChangedFor(nameof(HasNoMemories))]
    private int _memoriesCount;

    public bool HasDeceasedPhotos => DeceasedPhotosCount > 0;
    public bool HasGravePhotos => GravePhotosCount > 0;
    public bool HasDocuments => DocumentsCount > 0;

    public bool HasNoDeceasedPhotos => DeceasedPhotosCount == 0;
    public bool HasNoGravePhotos => GravePhotosCount == 0;
    public bool HasNoDocuments => DocumentsCount == 0;

    public bool HasMemories => MemoriesCount > 0;
    public bool HasNoMemories => MemoriesCount == 0;

    public string? MainPhotoUrl => Data?.MainPhotoUrl
                                   ?? DeceasedPhotos.FirstOrDefault(p => p.IsMainPhoto)?.Url
                                   ?? DeceasedPhotos.FirstOrDefault()?.Url;
    public bool HasMainPhoto => MainPhotoUrl is not null;
    public bool HasNoMainPhoto => MainPhotoUrl is null;

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

            var envelope = await deceasedApi.GetByIdAsync(id);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Карточка не найдена.";
                Data = null;
                return;
            }

            Data = envelope.Result;
            RebuildMemories();
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
    /// D30. Воспоминания приходят прямо в DeceasedDetailsResponse —
    /// отдельный endpoint не нужен. Перестраиваем коллекцию каждый раз
    /// заново (на десяток записей дешевле чем дифф).
    /// </summary>
    private void RebuildMemories()
    {
        Memories.Clear();
        if (Data?.Memories is not null)
        {
            foreach (var m in Data.Memories)
                Memories.Add(m);
        }
        MemoriesCount = Memories.Count;
    }

    /// <summary>
    /// D30. Удалить воспоминание — только админ. Бэк-endpoint позволяет
    /// удалять автору и админу; здесь мы всегда админ. Подтверждение —
    /// alert, обратно не вернуть.
    /// </summary>
    [RelayCommand]
    private async Task DeleteMemoryAsync(DeceasedMemoryResponse? memory)
    {
        if (memory is null || !Guid.TryParse(DeceasedId, out var deceasedId)) return;

        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var confirmed = await page.DisplayAlert(
            "Удалить воспоминание?",
            "Текст будет удалён без возможности восстановления.",
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

            // Перезагружаем всю карточку — Memories обновятся через
            // RebuildMemories. Лишний GET, но проще чем поддерживать
            // отдельный refresh-только-memories.
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

    private async Task LoadMediaAsync(Guid deceasedId)
    {
        try
        {
            var envelope = await mediaApi.GetListAsync(deceasedId, page: 1, pageSize: 100);
            if (envelope.Result is null) return;

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

            // Обновляем counter'ы — все Has*/HasNo* свойства
            // перерасчитываются автоматически через [NotifyPropertyChangedFor].
            DeceasedPhotosCount = DeceasedPhotos.Count;
            GravePhotosCount = GravePhotos.Count;
            DocumentsCount = Documents.Count;
        }
        catch
        {
            // Best-effort: если медиа не загрузились, остальное всё равно
            // должно работать. Юзер увидит пустые секции — не критично.
        }
    }

    /// <summary>
    /// ActionSheet "Что загрузить?" → фото умершего / фото могилы /
    /// документ. Логика 1-в-1 как в DeceasedDetailsViewModel.UploadFileAsync,
    /// но без auto-promote main: админ его сам поставит явно если нужно.
    /// </summary>
    [RelayCommand]
    private async Task UploadFileAsync()
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var choice = await page.DisplayActionSheet(
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
    /// Тап по карточке медиа — ActionSheet с действиями:
    /// "Открыть на весь экран" (D27.1), "Сделать главным" (только для
    /// DeceasedPhoto и только если ещё не main), "Удалить".
    /// </summary>
    [RelayCommand]
    private async Task PhotoTappedAsync(MediaListItem? item)
    {
        if (item is null || !Guid.TryParse(DeceasedId, out var deceasedId)) return;

        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var actions = new List<string> { "Открыть на весь экран" };
        var canBeMain = item.Kind == MediaKinds.DeceasedPhotoString && !item.IsMainPhoto;
        if (canBeMain) actions.Add("Сделать главным");
        actions.Add("Удалить");

        var choice = await page.DisplayActionSheet(
            item.OriginalFileName,
            "Отмена",
            null,
            actions.ToArray());

        switch (choice)
        {
            case "Открыть на весь экран":
                await OpenPhotoFullScreenAsync(item);
                break;
            case "Сделать главным":
                await SetMainPhotoAsync(deceasedId, item.Id);
                break;
            case "Удалить":
                await DeleteMediaAsync(item);
                break;
        }
    }

    private static async Task OpenPhotoFullScreenAsync(MediaListItem item)
    {
        if (string.IsNullOrEmpty(item.Url)) return;
        var encoded = Uri.EscapeDataString(item.Url);
        await Shell.Current.GoToAsync($"photo-viewer?url={encoded}");
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
            await LoadAsync();
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

    private async Task DeleteMediaAsync(MediaListItem item)
    {
        if (!Guid.TryParse(DeceasedId, out var deceasedId)) return;

        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var confirmed = await page.DisplayAlert(
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

            // Auto-promote: если только что загрузили фото умершего и
            // ни одно из фото пока не назначено главным — назначаем это
            // главным. Без этого в общем поиске (GET /api/deceased-records)
            // карточка останется без превью, потому что бэк отдаёт
            // MainPhotoUrl только когда MainMediaId явно задан.
            if (kind == MediaKinds.DeceasedPhoto
                && DeceasedPhotos.Count > 0
                && DeceasedPhotos.All(p => !p.IsMainPhoto))
            {
                try
                {
                    await mediaApi.SetMainPhotoAsync(deceasedId, envelope.Result.MediaId);
                    await LoadAsync();
                }
                catch
                {
                    // Назначение main не критично — фото загружено и видно
                    // в галерее. Админ сможет нажать "Сделать главным" вручную.
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

    /// <summary>
    /// Открыть редактор основных полей карточки (имя, даты, биография).
    /// Использует существующий edit-deceased flow; админ → 200 (бэк
    /// разрешает админу даже без tracking).
    /// </summary>
    [RelayCommand]
    private async Task EditDeceasedAsync()
    {
        if (!Guid.TryParse(DeceasedId, out _)) return;
        await Shell.Current.GoToAsync($"edit-deceased?deceasedId={DeceasedId}");
    }

    /// <summary>
    /// D29. Текст кнопки верификации зависит от текущего состояния:
    /// если карточка не проверена — "Подтвердить (verify)";
    /// если уже проверена — "Снять подтверждение (unverify)".
    /// Биндинг на свойство, которое пересчитывается через
    /// NotifyPropertyChangedFor на Data.
    /// </summary>
    public string VerifyButtonText => IsVerified
        ? "Снять подтверждение"
        : "Подтвердить карточку";

    /// <summary>
    /// D29. Переключает флаг IsVerified карточки. Если сейчас false —
    /// PUT /verify, если true — PUT /unverified. После успеха
    /// перезагружаем карточку, чтобы badge "проверено" и текст кнопки
    /// обновились. 409 от бэка (уже verified/unverified) показываем
    /// как обычную ошибку — это редкая гонка двух админов.
    /// </summary>
    [RelayCommand]
    private async Task ToggleVerifyAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var id) || Data is null) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var resp = IsVerified
                ? await deceasedApi.UnverifyAsync(id)
                : await deceasedApi.VerifyAsync(id);

            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = (int)resp.StatusCode switch
                {
                    403 => "Только администраторы могут менять статус проверки.",
                    404 => "Карточка не найдена.",
                    409 => IsVerified
                        ? "Карточка уже не подтверждена."
                        : "Карточка уже подтверждена.",
                    _ => $"Не удалось изменить статус (HTTP {(int)resp.StatusCode}).",
                };
                return;
            }

            await LoadAsync();
            OnPropertyChanged(nameof(VerifyButtonText));
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
    /// Редактирование координат — открывает burial-location-editor с
    /// предзаполненными значениями.
    /// </summary>
    [RelayCommand]
    private async Task EditCoordinatesAsync()
    {
        if (!Guid.TryParse(DeceasedId, out _) || Data is null) return;

        var lat = Data.Latitude?.ToString("0.000000", CultureInfo.InvariantCulture) ?? "";
        var lon = Data.Longitude?.ToString("0.000000", CultureInfo.InvariantCulture) ?? "";
        var acc = Data.AccuracyMeters?.ToString("0", CultureInfo.InvariantCulture) ?? "";

        await Shell.Current.GoToAsync(
            $"burial-location-editor?deceasedId={DeceasedId}&lat={lat}&lon={lon}&acc={acc}");
    }

    /// <summary>
    /// Открыть полную историю правок карточки (D24). Доступно админам.
    /// </summary>
    [RelayCommand]
    private async Task OpenEditsHistoryAsync()
    {
        if (!Guid.TryParse(DeceasedId, out _)) return;
        await Shell.Current.GoToAsync($"edits-history?deceasedId={DeceasedId}");
    }

    /// <summary>
    /// Жёсткое удаление карточки. Доступно только админу — бэк вернёт 403
    /// если нет роли. После удаления возвращаемся на список поиска.
    /// </summary>
    [RelayCommand]
    private async Task DeleteDeceasedAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var id) || Data is null) return;

        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var confirmed = await page.DisplayAlert(
            "Удалить карточку?",
            $"Карточка {FullName} будет удалена полностью, включая фото, документы и воспоминания. Действие необратимо.",
            "Удалить",
            "Отмена");
        if (!confirmed) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var resp = await deceasedApi.DeleteAsync(id);
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

            await Shell.Current.GoToAsync("..");
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
}
