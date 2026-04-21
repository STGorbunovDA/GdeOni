using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class DeceasedMemoryEntry : Entity<Guid>
{
    public string Text { get; private set; }
    public Guid? AuthorUserId { get; }
    public DateTime CreatedAtUtc { get; }
    public ModerationStatus ModerationStatus { get; private set; }

    private DeceasedMemoryEntry() : base(Guid.Empty)
    {
        Text = null!;
    }

    private DeceasedMemoryEntry(
        Guid id,
        string text,
        Guid? authorUserId,
        DateTime createdAtUtc) : base(id)
    {
        Text = text;
        AuthorUserId = authorUserId;
        CreatedAtUtc = createdAtUtc;
        ModerationStatus = ModerationStatus.Pending;
    }

    public static Result<DeceasedMemoryEntry, Error> Create(
        string text,
        Guid? authorUserId = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Errors.DeceasedMemory.TextRequired();

        return Result.Success<DeceasedMemoryEntry, Error>(new DeceasedMemoryEntry(
            Guid.NewGuid(),
            text.Trim(),
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
    

    public bool IsApproved() => ModerationStatus == ModerationStatus.Approved;
    public bool IsRejected() => ModerationStatus == ModerationStatus.Rejected;
    public bool IsPending() => ModerationStatus == ModerationStatus.Pending;
}