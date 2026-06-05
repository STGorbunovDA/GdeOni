using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class Deceased : Entity<Guid>
{
    public const int MaxShortDescriptionLength = 1000;
    public const int MaxBiographyLength = 10000;
    public const int MaxSearchKey = 1000;

    public PersonName Name { get; private set; }
    public LifePeriod LifePeriod { get; private set; }
    public BurialLocation? BurialLocation { get; private set; }

    public string? ShortDescription { get; private set; }
    public string? Biography { get; private set; }

    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid CreatedByUserId { get; }
    public bool IsVerified { get; private set; }

    private readonly List<DeceasedMemoryEntry> _memories = new();
    public IReadOnlyCollection<DeceasedMemoryEntry> Memories => _memories.AsReadOnly();

    private readonly List<DeceasedMedia> _media = new();
    public IReadOnlyCollection<DeceasedMedia> Media => _media.AsReadOnly();

    // D24. Audit log правок основных полей карточки (main-info, metadata,
    // burial-location). Создание самой карточки тут не фиксируется —
    // для него есть CreatedByUserId/CreatedAtUtc.
    private readonly List<DeceasedEdit> _edits = new();
    public IReadOnlyCollection<DeceasedEdit> Edits => _edits.AsReadOnly();

    public Guid? MainMediaId { get; private set; }
    public DeceasedMedia? MainMedia { get; private set; }

    public string SearchKey { get; private set; } = null!;
    public DeceasedMetadata Metadata { get; private set; }

    private Deceased() : base(Guid.Empty)
    {
        Name = null!;
        LifePeriod = null!;
        Metadata = DeceasedMetadata.Empty();
    }

    private Deceased(
        Guid id,
        PersonName name,
        LifePeriod lifePeriod,
        BurialLocation? burialLocation,
        string? shortDescription,
        string? biography,
        Guid createdByUserId,
        DateTime createdAtUtc) : base(id)
    {
        Name = name;
        LifePeriod = lifePeriod;
        BurialLocation = burialLocation;
        ShortDescription = shortDescription;
        Biography = biography;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        IsVerified = false;
        Metadata = DeceasedMetadata.Empty();
        RebuildSearchKey();
    }

    public static Result<Deceased, Error> Create(
        string firstName,
        string lastName,
        string? middleName,
        DateOnly? birthDate,
        DateOnly deathDate,
        BurialLocation? burialLocation,
        Guid createdByUserId,
        string? shortDescription = null,
        string? biography = null)
    {
        if (createdByUserId == Guid.Empty)
            return Errors.Deceased.CreatedByRequired();

        var nameResult = PersonName.Create(firstName, lastName, middleName);
        if (nameResult.IsFailure)
            return nameResult.Error;

        var periodResult = LifePeriod.Create(birthDate, deathDate);
        if (periodResult.IsFailure)
            return periodResult.Error;

        var shortDescriptionResult = NormalizeShortDescription(shortDescription);
        if (shortDescriptionResult.IsFailure)
            return shortDescriptionResult.Error;

        var biographyResult = NormalizeBiography(biography);
        if (biographyResult.IsFailure)
            return biographyResult.Error;

        return Result.Success<Deceased, Error>(
            new Deceased(
                Guid.NewGuid(),
                nameResult.Value,
                periodResult.Value,
                burialLocation,
                shortDescriptionResult.Value,
                biographyResult.Value,
                createdByUserId,
                DateTime.UtcNow));
    }

    public int? AgeAtDeath() => LifePeriod.AgeAtDeath();

    public DeceasedMedia? GetMainPhoto()
    {
        if (MainMediaId is null) return null;

        var photo = MainMedia ?? _media.FirstOrDefault(x => x.Id == MainMediaId);
        if (photo is null) return null;
        // Только Approved попадает в публичный URL карточки. Pending —
        // ещё не проверено модерацией, Rejected — отклонено. Защищает
        // от обхода типа "загрузил оскорбительное фото, отметил main —
        // оно сразу видно подписчикам".
        if (photo.ModerationStatus != ModerationStatus.Approved) return null;
        return photo;
    }

    /// <summary>
    /// Видимы ли клиенту воспоминания. Возвращает true, только если есть
    /// хотя бы одно Approved-воспоминание; Pending и Rejected не считаются
    /// (см. D11.4.7) — иначе подписчик видел бы "есть воспоминания",
    /// открывал карточку и обнаруживал пустоту.
    /// </summary>
    public bool HasMemories() =>
        _memories.Any(x => x.ModerationStatus == ModerationStatus.Approved);

    public UnitResult<Error> UpdateMainInfo(
        string firstName,
        string lastName,
        string? middleName,
        DateOnly? birthDate,
        DateOnly deathDate,
        string? shortDescription,
        string? biography,
        Guid? editorUserId = null)
    {
        var nameResult = PersonName.Create(firstName, lastName, middleName);
        if (nameResult.IsFailure)
            return nameResult.Error;

        var periodResult = LifePeriod.Create(birthDate, deathDate);
        if (periodResult.IsFailure)
            return periodResult.Error;

        var shortDescriptionResult = NormalizeShortDescription(shortDescription);
        if (shortDescriptionResult.IsFailure)
            return shortDescriptionResult.Error;

        var biographyResult = NormalizeBiography(biography);
        if (biographyResult.IsFailure)
            return biographyResult.Error;

        // No-op guard (D11.10.2): PUT с теми же значениями не должен
        // двигать UpdatedAtUtc и пересобирать SearchKey. Симметрично
        // ChangeBurialLocation (D11.4.5). PersonName/LifePeriod —
        // ValueObject, Equals структурный.
        if (Equals(Name, nameResult.Value) &&
            Equals(LifePeriod, periodResult.Value) &&
            ShortDescription == shortDescriptionResult.Value &&
            Biography == biographyResult.Value)
        {
            return UnitResult.Success<Error>();
        }

        // D24. Считаем diff ДО мутации, чтобы Old-значения отражали
        // реальное предыдущее состояние.
        var changes = new Dictionary<string, ChangePair>();
        if (!Equals(Name, nameResult.Value))
        {
            if (Name.FirstName != nameResult.Value.FirstName)
                changes["FirstName"] = new ChangePair(Name.FirstName, nameResult.Value.FirstName);
            if (Name.LastName != nameResult.Value.LastName)
                changes["LastName"] = new ChangePair(Name.LastName, nameResult.Value.LastName);
            if (Name.MiddleName != nameResult.Value.MiddleName)
                changes["MiddleName"] = new ChangePair(Name.MiddleName, nameResult.Value.MiddleName);
        }
        if (!Equals(LifePeriod, periodResult.Value))
        {
            if (LifePeriod.BirthDate != periodResult.Value.BirthDate)
                changes["BirthDate"] = new ChangePair(
                    LifePeriod.BirthDate?.ToString("yyyy-MM-dd"),
                    periodResult.Value.BirthDate?.ToString("yyyy-MM-dd"));
            if (LifePeriod.DeathDate != periodResult.Value.DeathDate)
                changes["DeathDate"] = new ChangePair(
                    LifePeriod.DeathDate.ToString("yyyy-MM-dd"),
                    periodResult.Value.DeathDate.ToString("yyyy-MM-dd"));
        }
        if (ShortDescription != shortDescriptionResult.Value)
            changes["ShortDescription"] = new ChangePair(ShortDescription, shortDescriptionResult.Value);
        if (Biography != biographyResult.Value)
            changes["Biography"] = new ChangePair(Biography, biographyResult.Value);

        Name = nameResult.Value;
        LifePeriod = periodResult.Value;
        ShortDescription = shortDescriptionResult.Value;
        Biography = biographyResult.Value;

        Touch();
        RebuildSearchKey();

        return RecordEditIfPossible(editorUserId, DeceasedEditKind.MainInfo, changes);
    }

    public UnitResult<Error> ChangeBurialLocation(BurialLocation? burialLocation, Guid? editorUserId = null)
    {
        // No-op guard: если новое значение совпадает со старым, не двигаем
        // UpdatedAtUtc и не пересобираем SearchKey — иначе любая повторная
        // запись из клиента дёргает БД зря (см. D11.4.5).
        if (Equals(BurialLocation, burialLocation))
            return UnitResult.Success<Error>();

        var changes = BuildBurialLocationDiff(BurialLocation, burialLocation);

        BurialLocation = burialLocation;
        Touch();
        RebuildSearchKey();

        return RecordEditIfPossible(editorUserId, DeceasedEditKind.BurialLocation, changes);
    }

    private static Dictionary<string, ChangePair> BuildBurialLocationDiff(
        BurialLocation? oldLocation,
        BurialLocation? newLocation)
    {
        var changes = new Dictionary<string, ChangePair>();
        string? OldS(Func<BurialLocation, string?> f) => oldLocation is null ? null : f(oldLocation);
        string? NewS(Func<BurialLocation, string?> f) => newLocation is null ? null : f(newLocation);

        void AddIfChanged(string key, string? oldV, string? newV)
        {
            if (oldV != newV)
                changes[key] = new ChangePair(oldV, newV);
        }

        AddIfChanged("Latitude", oldLocation?.Latitude.ToString("R"), newLocation?.Latitude.ToString("R"));
        AddIfChanged("Longitude", oldLocation?.Longitude.ToString("R"), newLocation?.Longitude.ToString("R"));
        AddIfChanged("AccuracyMeters", oldLocation?.AccuracyMeters?.ToString("R"), newLocation?.AccuracyMeters?.ToString("R"));
        AddIfChanged("Accuracy", oldLocation?.Accuracy.ToString(), newLocation?.Accuracy.ToString());
        AddIfChanged("Country", OldS(x => x.Country), NewS(x => x.Country));
        AddIfChanged("Region", OldS(x => x.Region), NewS(x => x.Region));
        AddIfChanged("City", OldS(x => x.City), NewS(x => x.City));
        AddIfChanged("CemeteryName", OldS(x => x.CemeteryName), NewS(x => x.CemeteryName));
        AddIfChanged("PlotNumber", OldS(x => x.PlotNumber), NewS(x => x.PlotNumber));
        AddIfChanged("GraveNumber", OldS(x => x.GraveNumber), NewS(x => x.GraveNumber));

        return changes;
    }

    public Result<DeceasedMemoryEntry, Error> AddMemory(
        string text,
        Guid? authorUserId = null)
    {
        var memoryResult = DeceasedMemoryEntry.Create(text, authorUserId);
        if (memoryResult.IsFailure)
            return memoryResult.Error;

        _memories.Add(memoryResult.Value);
        Touch();

        return Result.Success<DeceasedMemoryEntry, Error>(memoryResult.Value);
    }

    public UnitResult<Error> EditMemory(Guid memoryId, string text)
    {
        var memory = _memories.FirstOrDefault(x => x.Id == memoryId);
        if (memory is null)
            return Errors.DeceasedMemory.NotFound(memoryId);

        var result = memory.EditText(text);
        if (result.IsFailure)
            return result.Error;

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ApproveMemory(Guid memoryId)
    {
        var memory = _memories.FirstOrDefault(x => x.Id == memoryId);
        if (memory is null)
            return Errors.DeceasedMemory.NotFound(memoryId);

        var result = memory.Approve();
        if (result.IsFailure)
            return result.Error;

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RejectMemory(Guid memoryId)
    {
        var memory = _memories.FirstOrDefault(x => x.Id == memoryId);
        if (memory is null)
            return Errors.DeceasedMemory.NotFound(memoryId);

        var result = memory.Reject();
        if (result.IsFailure)
            return result.Error;

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RemoveMemory(Guid memoryId)
    {
        var memory = _memories.FirstOrDefault(x => x.Id == memoryId);
        if (memory is null)
            return Errors.DeceasedMemory.NotFound(memoryId);

        _memories.Remove(memory);
        Touch();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Verify()
    {
        if (IsVerified)
            return Errors.Deceased.AlreadyVerified();

        IsVerified = true;
        Touch();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Unverify()
    {
        if (!IsVerified)
            return Errors.Deceased.NotVerified();

        IsVerified = false;
        Touch();

        return UnitResult.Success<Error>();
    }

    public Result<DeceasedMedia, Error> AddMedia(
        Guid uploadedByUserId,
        MediaKind kind,
        string originalFileName,
        string bucket,
        string storageKey,
        string contentType,
        long sizeBytes,
        string? description = null)
    {
        // Уникальность storageKey гарантирована на двух уровнях, поэтому
        // здесь нет защитного _media.Any(StorageKey) — иначе UploadMedia
        // вынужден был бы Include(Media) ради линейного scan'а:
        //   1. MinioFileStorage.BuildObjectKey строит "<prefix>/<deceasedId>/<Guid>.<ext>"
        //      — Guid.NewGuid гарантирует уникальность;
        //   2. unique-индекс ux_deceased_media_storage_key в БД ловит race
        //      и пробрасывается через UniqueConstraint.FromName в DuplicateStorageKey.
        var mediaResult = DeceasedMedia.Create(
            Id,
            uploadedByUserId,
            kind,
            originalFileName,
            bucket,
            storageKey,
            contentType,
            sizeBytes,
            description);

        if (mediaResult.IsFailure)
            return mediaResult.Error;

        _media.Add(mediaResult.Value);
        Touch();

        return Result.Success<DeceasedMedia, Error>(mediaResult.Value);
    }

    public UnitResult<Error> RemoveMedia(Guid mediaId)
    {
        var media = _media.FirstOrDefault(x => x.Id == mediaId);
        if (media is null)
            return Errors.DeceasedMedia.NotFound(mediaId);

        if (MainMediaId == mediaId)
            ClearMainMedia();

        _media.Remove(media);
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> SetMainPhoto(Guid mediaId)
    {
        var media = _media.FirstOrDefault(x => x.Id == mediaId);
        if (media is null)
            return Errors.DeceasedMedia.NotFound(mediaId);

        if (media.Kind != MediaKind.DeceasedPhoto)
            return Errors.DeceasedMedia.OnlyDeceasedPhotoCanBeMain();

        // Только Approved фото может стать main. Защищает от сценария
        // "загрузил Pending → пометил main → ждёт Approve и автоматически
        // публикует main без явного решения админа". Юзер сначала
        // дожидается модерации, потом ставит main явно.
        if (media.ModerationStatus != ModerationStatus.Approved)
            return Errors.DeceasedMedia.MainPhotoMustBeApproved();

        foreach (var item in _media.Where(x => x.Kind == MediaKind.DeceasedPhoto && x.Id != mediaId))
            item.UnmarkMainPhoto();

        var markResult = media.MarkAsMainPhoto();
        if (markResult.IsFailure)
            return markResult.Error;

        SetMainMedia(media);
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> UpdateMediaDescription(Guid mediaId, string? description)
    {
        var media = _media.FirstOrDefault(x => x.Id == mediaId);
        if (media is null)
            return Errors.DeceasedMedia.NotFound(mediaId);

        var result = media.UpdateDescription(description);
        if (result.IsFailure)
            return result.Error;

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ApproveMedia(Guid mediaId)
    {
        var media = _media.FirstOrDefault(x => x.Id == mediaId);
        if (media is null)
            return Errors.DeceasedMedia.NotFound(mediaId);

        var result = media.Approve();
        if (result.IsFailure)
            return result.Error;

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RejectMedia(Guid mediaId)
    {
        var media = _media.FirstOrDefault(x => x.Id == mediaId);
        if (media is null)
            return Errors.DeceasedMedia.NotFound(mediaId);

        var result = media.Reject();
        if (result.IsFailure)
            return result.Error;

        if (MainMediaId == mediaId)
            ClearMainMedia();

        Touch();
        return UnitResult.Success<Error>();
    }

    // Единственная точка изменения связки MainMediaId/MainMedia
    // (D11.4.6): два поля синхронизируются вместе, риск дрейфа исключён
    // независимо от того, какой use case вызывает мутацию.
    private void SetMainMedia(DeceasedMedia media)
    {
        MainMediaId = media.Id;
        MainMedia = media;
    }

    private void ClearMainMedia()
    {
        MainMediaId = null;
        MainMedia = null;
    }

    public UnitResult<Error> UpdateMetadata(DeceasedMetadata metadata, Guid? editorUserId = null)
    {
        if (metadata is null)
            return Errors.Deceased.MetadataRequired();

        // No-op guard (D11.14.1): тот же metadata структурно — не двигаем
        // UpdatedAtUtc. ValueObject.Equals сравнивает по
        // GetEqualityComponents, без рассинхронизации после Trim.
        if (Equals(Metadata, metadata))
            return UnitResult.Success<Error>();

        var changes = BuildMetadataDiff(Metadata, metadata);

        Metadata = metadata;
        Touch();

        return RecordEditIfPossible(editorUserId, DeceasedEditKind.Metadata, changes);
    }

    public UnitResult<Error> ClearMetadata(Guid? editorUserId = null)
    {
        // No-op guard (D11.14.2): уже Empty — не двигаем UpdatedAtUtc.
        // Повторный DELETE /metadata идемпотентен и не должен дёргать БД.
        if (Metadata.IsEmpty())
            return UnitResult.Success<Error>();

        var empty = DeceasedMetadata.Empty();
        var changes = BuildMetadataDiff(Metadata, empty);

        Metadata = empty;
        Touch();

        return RecordEditIfPossible(editorUserId, DeceasedEditKind.Metadata, changes);
    }

    private static Dictionary<string, ChangePair> BuildMetadataDiff(DeceasedMetadata oldM, DeceasedMetadata newM)
    {
        var changes = new Dictionary<string, ChangePair>();
        void AddIfChanged(string key, string? oldV, string? newV)
        {
            if (oldV != newV)
                changes[key] = new ChangePair(oldV, newV);
        }

        AddIfChanged("Epitaph", oldM.Epitaph, newM.Epitaph);
        AddIfChanged("Religion", oldM.Religion, newM.Religion);
        AddIfChanged("Source", oldM.Source, newM.Source);
        AddIfChanged("AdditionalInfo", oldM.AdditionalInfo, newM.AdditionalInfo);
        if (oldM.IsMilitaryService != newM.IsMilitaryService)
            changes["IsMilitaryService"] = new ChangePair(
                oldM.IsMilitaryService.ToString().ToLowerInvariant(),
                newM.IsMilitaryService.ToString().ToLowerInvariant());

        return changes;
    }

    /// <summary>
    /// D24. Если редактор задан и есть реальные изменения — пишем edit
    /// в audit log. Иначе (editorUserId null — старый Create/CRUD-flow,
    /// либо changes пустой) — silent skip.
    /// </summary>
    private UnitResult<Error> RecordEditIfPossible(
        Guid? editorUserId,
        DeceasedEditKind kind,
        IReadOnlyDictionary<string, ChangePair> changes)
    {
        if (editorUserId is null || editorUserId == Guid.Empty || changes.Count == 0)
            return UnitResult.Success<Error>();

        var editResult = DeceasedEdit.Create(Id, editorUserId.Value, kind, changes);
        if (editResult.IsFailure)
            return editResult.Error;

        _edits.Add(editResult.Value);
        return UnitResult.Success<Error>();
    }

    private void RebuildSearchKey()
    {
        SearchKey = BuildSearchKey(
            Name.FirstName,
            Name.LastName,
            Name.MiddleName,
            LifePeriod.BirthDate,
            LifePeriod.DeathDate,
            BurialLocation);
    }

    private static string BuildSearchKey(
        string firstName,
        string lastName,
        string? middleName,
        DateOnly? birthDate,
        DateOnly deathDate,
        BurialLocation? burialLocation)
    {
        static string NormalizeString(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? "-"
                : value.Trim().ToUpperInvariant();

        static string NormalizeDate(DateOnly? value) =>
            value?.ToString("yyyy-MM-dd") ?? "-";

        return string.Join("|",
            NormalizeString(firstName),
            NormalizeString(lastName),
            NormalizeString(middleName),
            NormalizeDate(birthDate),
            NormalizeDate(deathDate),
            NormalizeString(burialLocation?.CemeteryName),
            NormalizeString(burialLocation?.City),
            NormalizeString(burialLocation?.PlotNumber),
            NormalizeString(burialLocation?.GraveNumber));
    }

    private static Result<string?, Error> NormalizeShortDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Success<string?, Error>(null);

        var normalized = value.Trim();

        if (normalized.Length > MaxShortDescriptionLength)
            return Errors.Deceased.ShortDescriptionTooLong(MaxShortDescriptionLength);

        return Result.Success<string?, Error>(normalized);
    }

    private static Result<string?, Error> NormalizeBiography(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Success<string?, Error>(null);

        var normalized = value.Trim();

        if (normalized.Length > MaxBiographyLength)
            return Errors.Deceased.BiographyTooLong(MaxBiographyLength);

        return Result.Success<string?, Error>(normalized);
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}