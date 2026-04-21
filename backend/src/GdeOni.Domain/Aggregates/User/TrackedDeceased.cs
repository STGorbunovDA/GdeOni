using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.User;

public sealed class TrackedDeceased : Entity<Guid>
{
    public Guid DeceasedId { get; }
    public RelationshipType RelationshipType { get; private set; }
    public string? PersonalNotes { get; private set; }
    public bool NotifyOnDeathAnniversary { get; private set; }
    public bool NotifyOnBirthAnniversary { get; private set; }
    public TrackStatus Status { get; private set; }
    public DateTime TrackedAtUtc { get; }

    private TrackedDeceased() : base(Guid.Empty)
    {
    }

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

    public static Result<TrackedDeceased, Error> Create(
        Guid deceasedId,
        RelationshipType relationshipType,
        string? personalNotes = null,
        bool notifyOnDeathAnniversary = false,
        bool notifyOnBirthAnniversary = false)
    {
        if (deceasedId == Guid.Empty)
            return Errors.Tracking.DeceasedIdRequired();

        return Result.Success<TrackedDeceased, Error>(new TrackedDeceased(
            Guid.NewGuid(),
            deceasedId,
            relationshipType,
            string.IsNullOrWhiteSpace(personalNotes) ? null : personalNotes.Trim(),
            notifyOnDeathAnniversary,
            notifyOnBirthAnniversary,
            TrackStatus.Active,
            DateTime.UtcNow));
    }

    public UnitResult<Error> UpdateRelationship(RelationshipType relationshipType, string? personalNotes)
    {
        RelationshipType = relationshipType;
        PersonalNotes = string.IsNullOrWhiteSpace(personalNotes) ? null : personalNotes.Trim();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangeNotifications(bool notifyOnDeathAnniversary, bool notifyOnBirthAnniversary)
    {
        NotifyOnDeathAnniversary = notifyOnDeathAnniversary;
        NotifyOnBirthAnniversary = notifyOnBirthAnniversary;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Archive()
    {
        if (Status == TrackStatus.Archived)
            return Errors.Tracking.AlreadyArchived();

        Status = TrackStatus.Archived;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Mute()
    {
        if (Status == TrackStatus.Muted)
            return Errors.Tracking.AlreadyMuted();

        Status = TrackStatus.Muted;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Activate()
    {
        if (Status == TrackStatus.Active)
            return Errors.Tracking.AlreadyActive();

        Status = TrackStatus.Active;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reactivate(
        RelationshipType relationshipType,
        string? personalNotes,
        bool notifyOnDeathAnniversary,
        bool notifyOnBirthAnniversary)
    {
        if (Status != TrackStatus.Archived)
            return Errors.Tracking.AlreadyTracked();

        RelationshipType = relationshipType;
        PersonalNotes = string.IsNullOrWhiteSpace(personalNotes) ? null : personalNotes.Trim();
        NotifyOnDeathAnniversary = notifyOnDeathAnniversary;
        NotifyOnBirthAnniversary = notifyOnBirthAnniversary;
        Status = TrackStatus.Active;

        return UnitResult.Success<Error>();
    }

    public bool IsActive() => Status == TrackStatus.Active;
    public bool IsMuted() => Status == TrackStatus.Muted;
    public bool IsArchived() => Status == TrackStatus.Archived;

    public bool HasNotificationsEnabled() =>
        NotifyOnDeathAnniversary || NotifyOnBirthAnniversary;
}