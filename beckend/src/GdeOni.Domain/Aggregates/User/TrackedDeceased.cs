using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.User;

public class TrackedDeceased : Entity<Guid>
{
    public Guid DeceasedId { get; }
    public RelationshipType RelationshipType { get; private set; }
    public string? PersonalNotes { get; private set; }
    public bool NotifyOnDeathAnniversary { get; private set; }
    public bool NotifyOnBirthAnniversary { get; private set; }
    public TrackStatus Status { get; private set; }
    public DateTime TrackedAtUtc { get; }

    private TrackedDeceased(
        Guid id,
        Guid deceasedId,
        RelationshipType relationshipType,
        string? personalNotes,
        bool notifyOnDeathAnniversary,
        bool notifyOnBirthAnniversary,
        TrackStatus status,
        DateTime trackedAtUtc) : base(id)
    {
        DeceasedId = deceasedId;
        RelationshipType = relationshipType;
        PersonalNotes = personalNotes;
        NotifyOnDeathAnniversary = notifyOnDeathAnniversary;
        NotifyOnBirthAnniversary = notifyOnBirthAnniversary;
        Status = status;
        TrackedAtUtc = trackedAtUtc;
    }

    public static Result<TrackedDeceased> Create(
        Guid deceasedId,
        RelationshipType relationshipType,
        string? personalNotes = null,
        bool notifyOnDeathAnniversary = false,
        bool notifyOnBirthAnniversary = false)
    {
        if (deceasedId == Guid.Empty)
            return Result.Failure<TrackedDeceased>("Id умершего обязателен");

        return Result.Success(new TrackedDeceased(
            Guid.NewGuid(),
            deceasedId,
            relationshipType,
            string.IsNullOrWhiteSpace(personalNotes) ? null : personalNotes.Trim(),
            notifyOnDeathAnniversary,
            notifyOnBirthAnniversary,
            TrackStatus.Active,
            DateTime.UtcNow));
    }

    public Result UpdateRelationship(RelationshipType relationshipType, string? personalNotes)
    {
        RelationshipType = relationshipType;
        PersonalNotes = string.IsNullOrWhiteSpace(personalNotes) ? null : personalNotes.Trim();
        return Result.Success();
    }

    public Result ChangeNotifications(bool notifyOnDeathAnniversary, bool notifyOnBirthAnniversary)
    {
        NotifyOnDeathAnniversary = notifyOnDeathAnniversary;
        NotifyOnBirthAnniversary = notifyOnBirthAnniversary;
        return Result.Success();
    }

    public Result Archive()
    {
        if (Status == TrackStatus.Archived)
            return Result.Failure("Запись уже в архиве");

        Status = TrackStatus.Archived;
        return Result.Success();
    }

    public Result Mute()
    {
        if (Status == TrackStatus.Muted)
            return Result.Failure("Уведомления уже отключены");

        Status = TrackStatus.Muted;
        return Result.Success();
    }

    public Result Activate()
    {
        if (Status == TrackStatus.Active)
            return Result.Failure("Отслеживание уже активно");

        Status = TrackStatus.Active;
        return Result.Success();
    }
}