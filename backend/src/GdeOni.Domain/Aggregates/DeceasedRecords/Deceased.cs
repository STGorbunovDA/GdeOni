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

    private readonly List<DeceasedPhoto> _photos = new();
    public IReadOnlyCollection<DeceasedPhoto> Photos => _photos.AsReadOnly();

    private readonly List<DeceasedMemoryEntry> _memories = new();
    public IReadOnlyCollection<DeceasedMemoryEntry> Memories => _memories.AsReadOnly();

    private readonly List<DeceasedMedia> _media = new();
    public IReadOnlyCollection<DeceasedMedia> Media => _media.AsReadOnly();

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

    public DeceasedPhoto? GetPrimaryPhoto() =>
        _photos.FirstOrDefault(x => x.IsPrimary);

    public bool HasPhotos() => _photos.Count > 0;

    public bool HasMemories() => _memories.Count > 0;

    public UnitResult<Error> UpdateMainInfo(
        string firstName,
        string lastName,
        string? middleName,
        DateOnly? birthDate,
        DateOnly deathDate,
        string? shortDescription,
        string? biography)
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

        Name = nameResult.Value;
        LifePeriod = periodResult.Value;
        ShortDescription = shortDescriptionResult.Value;
        Biography = biographyResult.Value;

        Touch();
        RebuildSearchKey();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangeBurialLocation(BurialLocation? burialLocation)
    {
        BurialLocation = burialLocation;
        Touch();
        RebuildSearchKey();

        return UnitResult.Success<Error>();
    }

    public Result<DeceasedPhoto, Error> AddPhoto(
        string url,
        Guid addedByUserId,
        string? description = null,
        bool makePrimary = false)
    {
        var normalizedUrlResult = DeceasedPhoto.NormalizeUrl(url);
        if (normalizedUrlResult.IsFailure)
            return normalizedUrlResult.Error;

        var normalizedUrl = normalizedUrlResult.Value;

        if (HasDuplicatePhotoUrl(normalizedUrl))
            return Errors.DeceasedPhoto.DuplicateUrl();

        var photoResult = DeceasedPhoto.Create(
            normalizedUrl,
            addedByUserId,
            description,
            makePrimary || _photos.Count == 0);

        if (photoResult.IsFailure)
            return photoResult.Error;

        var photo = photoResult.Value;

        if (photo.IsPrimary)
        {
            foreach (var item in _photos)
                item.UnmarkPrimary();
        }

        _photos.Add(photo);
        Touch();

        return Result.Success<DeceasedPhoto, Error>(photo);
    }

    public UnitResult<Error> UpdatePhotoUrl(Guid photoId, string url)
    {
        var photo = _photos.FirstOrDefault(x => x.Id == photoId);
        if (photo is null)
            return Errors.DeceasedPhoto.NotFound(photoId);

        var normalizedUrlResult = DeceasedPhoto.NormalizeUrl(url);
        if (normalizedUrlResult.IsFailure)
            return normalizedUrlResult.Error;

        var normalizedUrl = normalizedUrlResult.Value;

        if (HasDuplicatePhotoUrl(normalizedUrl, photoId))
            return Errors.DeceasedPhoto.DuplicateUrl();

        var result = photo.UpdateUrl(normalizedUrl);
        if (result.IsFailure)
            return result.Error;

        Touch();
        return UnitResult.Success<Error>();
    }

    private bool HasDuplicatePhotoUrl(string normalizedUrl, Guid? excludingPhotoId = null)
    {
        return _photos.Any(x =>
            x.Id != excludingPhotoId &&
            string.Equals(x.Url, normalizedUrl, StringComparison.OrdinalIgnoreCase));
    }

    public UnitResult<Error> SetPrimaryPhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(x => x.Id == photoId);
        if (photo is null)
            return Errors.DeceasedPhoto.NotFound(photoId);

        foreach (var item in _photos)
            item.UnmarkPrimary();

        var makePrimaryResult = photo.MakePrimary();
        if (makePrimaryResult.IsFailure)
            return makePrimaryResult.Error;

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> UpdatePhotoDescription(Guid photoId, string? description)
    {
        var photo = _photos.FirstOrDefault(x => x.Id == photoId);
        if (photo is null)
            return Errors.DeceasedPhoto.NotFound(photoId);

        var result = photo.UpdateDescription(description);
        if (result.IsFailure)
            return result.Error;

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ApprovePhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(x => x.Id == photoId);
        if (photo is null)
            return Errors.DeceasedPhoto.NotFound(photoId);

        var result = photo.Approve();
        if (result.IsFailure)
            return result.Error;

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RejectPhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(x => x.Id == photoId);
        if (photo is null)
            return Errors.DeceasedPhoto.NotFound(photoId);

        var result = photo.Reject();
        if (result.IsFailure)
            return result.Error;

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RemovePhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(x => x.Id == photoId);
        if (photo is null)
            return Errors.DeceasedPhoto.NotFound(photoId);

        _photos.Remove(photo);

        if (_photos.Count > 0 && _photos.All(x => !x.IsPrimary))
        {
            var makePrimaryResult = _photos[0].MakePrimary();
            if (makePrimaryResult.IsFailure)
                return makePrimaryResult.Error;
        }

        Touch();
        return UnitResult.Success<Error>();
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
        if (_media.Any(x => string.Equals(x.StorageKey, storageKey, StringComparison.OrdinalIgnoreCase)))
            return Errors.DeceasedMedia.DuplicateStorageKey();

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

        foreach (var item in _media.Where(x => x.Kind == MediaKind.DeceasedPhoto && x.Id != mediaId))
            item.UnmarkMainPhoto();

        var markResult = media.MarkAsMainPhoto();
        if (markResult.IsFailure)
            return markResult.Error;

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

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> UpdateMetadata(DeceasedMetadata metadata)
    {
        if (metadata is null)
            return Errors.Deceased.MetadataRequired();

        Metadata = metadata;
        Touch();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ClearMetadata()
    {
        Metadata = DeceasedMetadata.Empty();
        Touch();

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