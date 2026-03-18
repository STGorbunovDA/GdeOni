using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Deceased;

public sealed class DeceasedMemoryEntry : Entity<Guid>
{
    public string Text { get; private set; }
    public string AuthorDisplayName { get; private set; }
    public Guid? AuthorUserId { get; }
    public DateTime CreatedAtUtc { get; }
    public ModerationStatus ModerationStatus { get; private set; }

    private DeceasedMemoryEntry() : base(Guid.Empty)
    {
        Text = null!;
        AuthorDisplayName = null!;
    }

    private DeceasedMemoryEntry(
        Guid id,
        string text,
        string authorDisplayName,
        Guid? authorUserId,
        DateTime createdAtUtc) : base(id)
    {
        Text = text;
        AuthorDisplayName = authorDisplayName;
        AuthorUserId = authorUserId;
        CreatedAtUtc = createdAtUtc;
        ModerationStatus = ModerationStatus.Pending;
    }

    public static Result<DeceasedMemoryEntry, Error> Create(
        string text,
        string? authorDisplayName = null,
        Guid? authorUserId = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Errors.DeceasedMemory.TextRequired();

        return Result.Success<DeceasedMemoryEntry, Error>(new DeceasedMemoryEntry(
            Guid.NewGuid(),
            text.Trim(),
            string.IsNullOrWhiteSpace(authorDisplayName) ? "Аноним" : authorDisplayName.Trim(),
            authorUserId,
            DateTime.UtcNow));
    }

    public UnitResult<Error> EditText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Errors.DeceasedMemory.TextRequired();

        Text = text.Trim();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Approve()
    {
        if (ModerationStatus == ModerationStatus.Approved)
            return Errors.DeceasedMemory.AlreadyApproved();

        ModerationStatus = ModerationStatus.Approved;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reject()
    {
        if (ModerationStatus == ModerationStatus.Rejected)
            return Errors.DeceasedMemory.AlreadyRejected();

        ModerationStatus = ModerationStatus.Rejected;
        return UnitResult.Success<Error>();
    }
}