using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Deceased;

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

    public static Result<DeceasedPhoto> Create(
        string url,
        Guid addedByUserId,
        string? description = null,
        bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result.Failure<DeceasedPhoto>("URL фото обязателен");

        if (addedByUserId == Guid.Empty)
            return Result.Failure<DeceasedPhoto>("Пользователь, добавивший фото, обязателен");

        return Result.Success(new DeceasedPhoto(
            Guid.NewGuid(),
            url.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            isPrimary,
            addedByUserId,
            DateTime.UtcNow));
    }

    public Result MakePrimary()
    {
        if (IsPrimary)
            return Result.Failure("Фото уже является основным");

        IsPrimary = true;
        return Result.Success();
    }

    public void UnmarkPrimary()
    {
        IsPrimary = false;
    }

    public Result Approve()
    {
        if (ModerationStatus == ModerationStatus.Approved)
            return Result.Failure("Фото уже подтверждено");

        ModerationStatus = ModerationStatus.Approved;
        return Result.Success();
    }

    public Result Reject()
    {
        if (ModerationStatus == ModerationStatus.Rejected)
            return Result.Failure("Фото уже отклонено");

        ModerationStatus = ModerationStatus.Rejected;
        return Result.Success();
    }

    public Result UpdateDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        return Result.Success();
    }
}