using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class DeceasedPhoto : Entity<Guid>
{
    public const int MaxUrlLength = 2000;
    public const int MaxDescriptionLength = 1000;

    public string Url { get; private set; }
    public string? Description { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public Guid AddedByUserId { get; }
    public ModerationStatus ModerationStatus { get; private set; }

    private DeceasedPhoto() : base(Guid.Empty)
    {
        Url = null!;
    }

    private DeceasedPhoto(
        Guid id,
        string url,
        string? description,
        bool isPrimary,
        Guid addedByUserId,
        DateTime createdAtUtc) : base(id)
    {
        Url = url;
        Description = description;
        IsPrimary = isPrimary;
        AddedByUserId = addedByUserId;
        CreatedAtUtc = createdAtUtc;
        ModerationStatus = ModerationStatus.Pending;
    }

    public static Result<DeceasedPhoto, Error> Create(
        string url,
        Guid addedByUserId,
        string? description = null,
        bool isPrimary = false)
    {
        if (addedByUserId == Guid.Empty)
            return Errors.DeceasedPhoto.AddedByRequired();

        var urlResult = NormalizeAndValidateUrl(url);
        if (urlResult.IsFailure)
            return urlResult.Error;

        var descriptionResult = NormalizeAndValidateDescription(description);
        if (descriptionResult.IsFailure)
            return descriptionResult.Error;

        return Result.Success<DeceasedPhoto, Error>(
            new DeceasedPhoto(
                Guid.NewGuid(),
                urlResult.Value,
                descriptionResult.Value,
                isPrimary,
                addedByUserId,
                DateTime.UtcNow));
    }

    public UnitResult<Error> MakePrimary()
    {
        if (IsPrimary)
            return Errors.DeceasedPhoto.AlreadyPrimary();

        IsPrimary = true;
        return UnitResult.Success<Error>();
    }

    public void UnmarkPrimary()
    {
        IsPrimary = false;
    }

    public UnitResult<Error> UpdateDescription(string? description)
    {
        var descriptionResult = NormalizeAndValidateDescription(description);
        if (descriptionResult.IsFailure)
            return descriptionResult.Error;

        Description = descriptionResult.Value;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> UpdateUrl(string url)
    {
        var urlResult = NormalizeAndValidateUrl(url);
        if (urlResult.IsFailure)
            return urlResult.Error;

        Url = urlResult.Value;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Approve()
    {
        if (ModerationStatus == ModerationStatus.Approved)
            return Errors.DeceasedPhoto.AlreadyApproved();

        ModerationStatus = ModerationStatus.Approved;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reject()
    {
        if (ModerationStatus == ModerationStatus.Rejected)
            return Errors.DeceasedPhoto.AlreadyRejected();

        ModerationStatus = ModerationStatus.Rejected;
        return UnitResult.Success<Error>();
    }

    private static Result<string, Error> NormalizeAndValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Errors.DeceasedPhoto.UrlRequired();

        var normalized = url.Trim();

        if (normalized.Length > MaxUrlLength)
            return Errors.DeceasedPhoto.UrlTooLong(MaxUrlLength);

        if (!Uri.IsWellFormedUriString(normalized, UriKind.Absolute))
            return Errors.DeceasedPhoto.UrlInvalid();

        return Result.Success<string, Error>(normalized);
    }

    private static Result<string?, Error> NormalizeAndValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result.Success<string?, Error>(null);

        var normalized = description.Trim();

        if (normalized.Length > MaxDescriptionLength)
            return Errors.DeceasedPhoto.DescriptionTooLong(MaxDescriptionLength);

        return Result.Success<string?, Error>(normalized);
    }
}