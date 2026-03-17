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

    public static Result<DeceasedMemoryEntry> Create(
        string text,
        string? authorDisplayName = null,
        Guid? authorUserId = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<DeceasedMemoryEntry>("Текст воспоминания обязателен");

        return Result.Success(new DeceasedMemoryEntry(
            Guid.NewGuid(),
            text.Trim(),
            string.IsNullOrWhiteSpace(authorDisplayName) ? "Аноним" : authorDisplayName.Trim(),
            authorUserId,
            DateTime.UtcNow));
    }

    public Result EditText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure("Текст воспоминания обязателен");

        Text = text.Trim();
        return Result.Success();
    }

    public Result Approve()
    {
        if (ModerationStatus == ModerationStatus.Approved)
            return Result.Failure("Воспоминание уже подтверждено");

        ModerationStatus = ModerationStatus.Approved;
        return Result.Success();
    }

    public Result Reject()
    {
        if (ModerationStatus == ModerationStatus.Rejected)
            return Result.Failure("Воспоминание уже отклонено");

        ModerationStatus = ModerationStatus.Rejected;
        return Result.Success();
    }
}