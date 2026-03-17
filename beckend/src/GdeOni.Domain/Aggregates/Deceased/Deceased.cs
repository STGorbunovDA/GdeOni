using CSharpFunctionalExtensions;

namespace GdeOni.Domain.Aggregates.Deceased;

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
    }

    public static Result<Deceased> Create(
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
            return Result.Failure<Deceased>("Пользователь-создатель обязателен");

        var nameResult = PersonName.Create(firstName, lastName, middleName);
        if (nameResult.IsFailure)
            return Result.Failure<Deceased>(nameResult.Error);

        var periodResult = LifePeriod.Create(birthDate, deathDate);
        if (periodResult.IsFailure)
            return Result.Failure<Deceased>(periodResult.Error);

        if (burialLocation is null)
            return Result.Failure<Deceased>("Место захоронения обязательно");

        var deceased = new Deceased(
            Guid.NewGuid(),
            nameResult.Value,
            periodResult.Value,
            burialLocation,
            string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim(),
            string.IsNullOrWhiteSpace(biography) ? null : biography.Trim(),
            createdByUserId,
            DateTime.UtcNow);

        return Result.Success(deceased);
    }

    public Result UpdateMainInfo(
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
            return Result.Failure(nameResult.Error);

        var periodResult = LifePeriod.Create(birthDate, deathDate);
        if (periodResult.IsFailure)
            return Result.Failure(periodResult.Error);

        Name = nameResult.Value;
        LifePeriod = periodResult.Value;
        ShortDescription = string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim();
        Biography = string.IsNullOrWhiteSpace(biography) ? null : biography.Trim();
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    public Result ChangeBurialLocation(BurialLocation burialLocation)
    {
        if (burialLocation is null)
            return Result.Failure("Место захоронения обязательно");

        BurialLocation = burialLocation;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    public Result<DeceasedPhoto> AddPhoto(
        string url,
        Guid addedByUserId,
        string? description = null,
        bool makePrimary = false)
    {
        var photoResult = DeceasedPhoto.Create(url, addedByUserId, description, makePrimary);
        if (photoResult.IsFailure)
            return Result.Failure<DeceasedPhoto>(photoResult.Error);

        var photo = photoResult.Value;

        if (makePrimary)
        {
            foreach (var item in _photos)
                item.UnmarkPrimary();
        }

        _photos.Add(photo);
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success(photo);
    }

    public Result SetPrimaryPhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(x => x.Id == photoId);
        if (photo is null)
            return Result.Failure("Фото не найдено");

        foreach (var item in _photos)
            item.UnmarkPrimary();

        var makePrimaryResult = photo.MakePrimary();
        if (makePrimaryResult.IsFailure)
            return makePrimaryResult;

        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RemovePhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(x => x.Id == photoId);
        if (photo is null)
            return Result.Failure("Фото не найдено");

        _photos.Remove(photo);

        if (_photos.Count > 0 && _photos.All(x => !x.IsPrimary))
            _photos[0].MakePrimary();

        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result<DeceasedMemoryEntry> AddMemory(
        string text,
        string? authorDisplayName = null,
        Guid? authorUserId = null)
    {
        var memoryResult = DeceasedMemoryEntry.Create(text, authorDisplayName, authorUserId);
        if (memoryResult.IsFailure)
            return Result.Failure<DeceasedMemoryEntry>(memoryResult.Error);

        _memories.Add(memoryResult.Value);
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success(memoryResult.Value);
    }

    public Result RemoveMemory(Guid memoryId)
    {
        var memory = _memories.FirstOrDefault(x => x.Id == memoryId);
        if (memory is null)
            return Result.Failure("Воспоминание не найдено");

        _memories.Remove(memory);
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    public Result Verify()
    {
        if (IsVerified)
            return Result.Failure("Запись уже подтверждена");

        IsVerified = true;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    public Result Unverify()
    {
        if (!IsVerified)
            return Result.Failure("Запись еще не подтверждена");

        IsVerified = false;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }
    
    public Result UpdateMetadata(DeceasedMetadata metadata)
    {
        if (metadata is null)
            return Result.Failure("Метаданные обязательны");

        Metadata = metadata;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }
}