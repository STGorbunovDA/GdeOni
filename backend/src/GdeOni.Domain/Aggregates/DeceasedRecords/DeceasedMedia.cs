using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class DeceasedMedia : Entity<Guid>
{
    public const int MaxOriginalFileNameLength = 500;
    public const int MaxBucketLength = 100;
    public const int MaxStorageKeyLength = 500;
    public const int MaxContentTypeLength = 200;
    public const int MaxDescriptionLength = 1000;

    public Guid DeceasedId { get; }
    public Guid UploadedByUserId { get; }
    public MediaKind Kind { get; }
    public string OriginalFileName { get; private set; }
    public string Bucket { get; }
    public string StorageKey { get; }
    public string ContentType { get; }
    public long SizeBytes { get; }
    public string? Description { get; private set; }
    public bool IsMainPhoto { get; private set; }
    public ModerationStatus ModerationStatus { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private DeceasedMedia() : base(Guid.Empty)
    {
        OriginalFileName = null!;
        Bucket = null!;
        StorageKey = null!;
        ContentType = null!;
    }

    private DeceasedMedia(
        Guid id,
        Guid deceasedId,
        Guid uploadedByUserId,
        MediaKind kind,
        string originalFileName,
        string bucket,
        string storageKey,
        string contentType,
        long sizeBytes,
        string? description,
        DateTime createdAtUtc) : base(id)
    {
        DeceasedId = deceasedId;
        UploadedByUserId = uploadedByUserId;
        Kind = kind;
        OriginalFileName = originalFileName;
        Bucket = bucket;
        StorageKey = storageKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Description = description;
        IsMainPhoto = false;
        ModerationStatus = ModerationStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<DeceasedMedia, Error> Create(
        Guid deceasedId,
        Guid uploadedByUserId,
        MediaKind kind,
        string originalFileName,
        string bucket,
        string storageKey,
        string contentType,
        long sizeBytes,
        string? description = null)
    {
        if (deceasedId == Guid.Empty)
            return Errors.DeceasedMedia.DeceasedIdRequired();

        if (uploadedByUserId == Guid.Empty)
            return Errors.DeceasedMedia.UploadedByRequired();

        if (!Enum.IsDefined(typeof(MediaKind), kind))
            return Errors.DeceasedMedia.KindInvalid();

        if (sizeBytes <= 0)
            return Errors.DeceasedMedia.SizeBytesInvalid();

        var nameResult = NormalizeFileName(originalFileName);
        if (nameResult.IsFailure)
            return nameResult.Error;

        var bucketResult = NormalizeBucket(bucket);
        if (bucketResult.IsFailure)
            return bucketResult.Error;

        var keyResult = NormalizeStorageKey(storageKey);
        if (keyResult.IsFailure)
            return keyResult.Error;

        var contentTypeResult = NormalizeContentType(contentType);
        if (contentTypeResult.IsFailure)
            return contentTypeResult.Error;

        var descriptionResult = NormalizeDescription(description);
        if (descriptionResult.IsFailure)
            return descriptionResult.Error;

        return Result.Success<DeceasedMedia, Error>(
            new DeceasedMedia(
                Guid.NewGuid(),
                deceasedId,
                uploadedByUserId,
                kind,
                nameResult.Value,
                bucketResult.Value,
                keyResult.Value,
                contentTypeResult.Value,
                sizeBytes,
                descriptionResult.Value,
                DateTime.UtcNow));
    }

    internal UnitResult<Error> MarkAsMainPhoto()
    {
        if (Kind != MediaKind.DeceasedPhoto)
            return Errors.DeceasedMedia.OnlyDeceasedPhotoCanBeMain();

        IsMainPhoto = true;
        Touch();
        return UnitResult.Success<Error>();
    }

    internal void UnmarkMainPhoto()
    {
        if (!IsMainPhoto)
            return;

        IsMainPhoto = false;
        Touch();
    }

    public UnitResult<Error> UpdateDescription(string? description)
    {
        var descriptionResult = NormalizeDescription(description);
        if (descriptionResult.IsFailure)
            return descriptionResult.Error;

        Description = descriptionResult.Value;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Approve()
    {
        if (ModerationStatus == ModerationStatus.Approved)
            return Errors.DeceasedMedia.AlreadyApproved();

        ModerationStatus = ModerationStatus.Approved;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reject()
    {
        if (ModerationStatus == ModerationStatus.Rejected)
            return Errors.DeceasedMedia.AlreadyRejected();

        ModerationStatus = ModerationStatus.Rejected;
        Touch();
        return UnitResult.Success<Error>();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static Result<string, Error> NormalizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Errors.DeceasedMedia.OriginalFileNameRequired();

        var normalized = value.Trim();
        if (normalized.Length > MaxOriginalFileNameLength)
            return Errors.DeceasedMedia.OriginalFileNameTooLong(MaxOriginalFileNameLength);

        return Result.Success<string, Error>(normalized);
    }

    private static Result<string, Error> NormalizeBucket(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Errors.DeceasedMedia.BucketRequired();

        var normalized = value.Trim();
        if (normalized.Length > MaxBucketLength)
            return Errors.DeceasedMedia.BucketTooLong(MaxBucketLength);

        return Result.Success<string, Error>(normalized);
    }

    private static Result<string, Error> NormalizeStorageKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Errors.DeceasedMedia.StorageKeyRequired();

        var normalized = value.Trim();
        if (normalized.Length > MaxStorageKeyLength)
            return Errors.DeceasedMedia.StorageKeyTooLong(MaxStorageKeyLength);

        return Result.Success<string, Error>(normalized);
    }

    private static Result<string, Error> NormalizeContentType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Errors.DeceasedMedia.ContentTypeRequired();

        var normalized = value.Trim();
        if (normalized.Length > MaxContentTypeLength)
            return Errors.DeceasedMedia.ContentTypeTooLong(MaxContentTypeLength);

        return Result.Success<string, Error>(normalized);
    }

    private static Result<string?, Error> NormalizeDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Success<string?, Error>(null);

        var normalized = value.Trim();
        if (normalized.Length > MaxDescriptionLength)
            return Errors.DeceasedMedia.DescriptionTooLong(MaxDescriptionLength);

        return Result.Success<string?, Error>(normalized);
    }
}
