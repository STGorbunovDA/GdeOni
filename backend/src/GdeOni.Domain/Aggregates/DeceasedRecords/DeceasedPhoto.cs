using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class DeceasedPhoto : Entity<Guid>
{
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
        if (string.IsNullOrWhiteSpace(url))
            return Errors.DeceasedPhoto.UrlRequired();

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return Errors.DeceasedPhoto.UrlInvalid();

        if (addedByUserId == Guid.Empty)
            return Errors.DeceasedPhoto.AddedByRequired();

        return Result.Success<DeceasedPhoto, Error>(new DeceasedPhoto(
            Guid.NewGuid(),
            url.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
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

    public UnitResult<Error> UpdateDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        return UnitResult.Success<Error>();
    }
    
    public UnitResult<Error> UpdateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Errors.DeceasedPhoto.UrlRequired();

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return Errors.DeceasedPhoto.UrlInvalid();

        Url = url.Trim();
        return UnitResult.Success<Error>();
    }
}