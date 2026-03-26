using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class Deceased : Entity<Guid>
{
    public PersonName Name { get; private set; }
    public LifePeriod LifePeriod { get; private set; }
    public BurialLocation BurialLocation { get; private set; }

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
    public string SearchKey { get; private set; } = null!;

    public DeceasedMetadata Metadata { get; private set; }

    private Deceased() : base(Guid.Empty)
    {
        Name = null!;
        LifePeriod = null!;
        BurialLocation = null!;
        Metadata = DeceasedMetadata.Empty();
    }

    private Deceased(
        Guid id,
        PersonName name,
        LifePeriod lifePeriod,
        BurialLocation burialLocation,
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
        DateTime? birthDate,
        DateTime deathDate,
        BurialLocation burialLocation,
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

        if (burialLocation is null)
            return Errors.Deceased.BurialLocationRequired();

        var deceased = new Deceased(
            Guid.NewGuid(),
            nameResult.Value,
            periodResult.Value,
            burialLocation,
            string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim(),
            string.IsNullOrWhiteSpace(biography) ? null : biography.Trim(),
            createdByUserId,
            DateTime.UtcNow);
        
        
        return Result.Success<Deceased, Error>(deceased);
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
        DateTime? birthDate,
        DateTime deathDate,
        BurialLocation burialLocation)
    {
        static string N(string? value) => string.IsNullOrWhiteSpace(value)
            ? "-"
            : value.Trim().ToUpperInvariant();

        static string D(DateTime? value) => value?.Date.ToString("yyyy-MM-dd") ?? "-";

        return string.Join("|",
            N(firstName),
            N(lastName),
            N(middleName),
            D(birthDate),
            D(deathDate),
            N(burialLocation.CemeteryName),
            N(burialLocation.PlotNumber),
            N(burialLocation.GraveNumber));
    }

    public UnitResult<Error> UpdateMainInfo(
        string firstName,
        string lastName,
        string? middleName,
        DateTime? birthDate,
        DateTime deathDate,
        string? shortDescription,
        string? biography)
    {
        var nameResult = PersonName.Create(firstName, lastName, middleName);
        if (nameResult.IsFailure)
            return nameResult.Error;

        var periodResult = LifePeriod.Create(birthDate, deathDate);
        if (periodResult.IsFailure)
            return periodResult.Error;

        Name = nameResult.Value;
        LifePeriod = periodResult.Value;
        ShortDescription = string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim();
        Biography = string.IsNullOrWhiteSpace(biography) ? null : biography.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        RebuildSearchKey();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangeBurialLocation(BurialLocation burialLocation)
    {
        if (burialLocation is null)
            return Errors.Deceased.BurialLocationRequired();

        BurialLocation = burialLocation;
        UpdatedAtUtc = DateTime.UtcNow;
        
        RebuildSearchKey();

        return UnitResult.Success<Error>();
    }

    public Result<DeceasedPhoto, Error> AddPhoto(
        string url,
        Guid addedByUserId,
        string? description = null,
        bool makePrimary = false)
    {
        var shouldBePrimary = makePrimary || _photos.Count == 0;

        var photoResult = DeceasedPhoto.Create(url, addedByUserId, description, shouldBePrimary);
        if (photoResult.IsFailure)
            return photoResult.Error;

        var photo = photoResult.Value;

        if (photo.IsPrimary)
        {
            foreach (var item in _photos)
                item.UnmarkPrimary();
        }

        _photos.Add(photo);
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success<DeceasedPhoto, Error>(photo);
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

        UpdatedAtUtc = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RemovePhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(x => x.Id == photoId);
        if (photo is null)
            return Errors.DeceasedPhoto.NotFound(photoId);

        _photos.Remove(photo);

        if (_photos.Count > 0 && _photos.All(x => !x.IsPrimary))
            _photos[0].MakePrimary();

        UpdatedAtUtc = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }

    public Result<DeceasedMemoryEntry, Error> AddMemory(
        string text,
        string? authorDisplayName = null,
        Guid? authorUserId = null)
    {
        var memoryResult = DeceasedMemoryEntry.Create(text, authorDisplayName, authorUserId);
        if (memoryResult.IsFailure)
            return memoryResult.Error;

        _memories.Add(memoryResult.Value);
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success<DeceasedMemoryEntry, Error>(memoryResult.Value);
    }

    public UnitResult<Error> RemoveMemory(Guid memoryId)
    {
        var memory = _memories.FirstOrDefault(x => x.Id == memoryId);
        if (memory is null)
            return Errors.DeceasedMemory.NotFound(memoryId);

        _memories.Remove(memory);
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Verify()
    {
        if (IsVerified)
            return Errors.Deceased.AlreadyVerified();

        IsVerified = true;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Unverify()
    {
        if (!IsVerified)
            return Errors.Deceased.NotVerified();

        IsVerified = false;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> UpdateMetadata(DeceasedMetadata metadata)
    {
        if (metadata is null)
            return Errors.Deceased.MetadataRequired();

        Metadata = metadata;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }
}