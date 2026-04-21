using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class DeceasedMemoryEntry : Entity<Guid>
{
    public const int MaxTextLength = 5000;

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
        if (authorUserId.HasValue && authorUserId.Value == Guid.Empty)
            return Errors.User.IdRequired();

        var textResult = NormalizeAndValidateText(text);
        if (textResult.IsFailure)
            return textResult.Error;

        return Result.Success<DeceasedMemoryEntry, Error>(
            new DeceasedMemoryEntry(
                Guid.NewGuid(),
                textResult.Value,
                authorUserId,
                DateTime.UtcNow));
    }

    public UnitResult<Error> EditText(string text)
    {
        var textResult = NormalizeAndValidateText(text);
        if (textResult.IsFailure)
            return textResult.Error;

        Text = textResult.Value;
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

    private static Result<string, Error> NormalizeAndValidateText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Errors.DeceasedMemory.TextRequired();

        var normalized = text.Trim();

        if (normalized.Length > MaxTextLength)
            return Errors.DeceasedMemory.TextTooLong(MaxTextLength);

        return Result.Success<string, Error>(normalized);
    }
}