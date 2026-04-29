using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.User;

public sealed class TrackedDeceased : Entity<Guid>
{
    public const int MaxPersonalNotesLength = 2000;

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

        if (!Enum.IsDefined(typeof(RelationshipType), relationshipType))
            return Errors.Tracking.RelationshipTypeInvalid();

        var notesResult = NormalizePersonalNotes(personalNotes);
        if (notesResult.IsFailure)
            return notesResult.Error;

        return Result.Success<TrackedDeceased, Error>(
            new TrackedDeceased(
                Guid.NewGuid(),
                deceasedId,
                relationshipType,
                notesResult.Value,
                notifyOnDeathAnniversary,
                notifyOnBirthAnniversary,
                TrackStatus.Active,
                DateTime.UtcNow));
    }

    public UnitResult<Error> UpdateRelationship(
        RelationshipType relationshipType,
        string? personalNotes)
    {
        if (!Enum.IsDefined(typeof(RelationshipType), relationshipType))
            return Errors.Tracking.RelationshipTypeInvalid();

        var notesResult = NormalizePersonalNotes(personalNotes);
        if (notesResult.IsFailure)
            return notesResult.Error;

        RelationshipType = relationshipType;
        PersonalNotes = notesResult.Value;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangeNotifications(
        bool notifyOnDeathAnniversary,
        bool notifyOnBirthAnniversary)
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
        var updateRelationshipResult = UpdateRelationship(relationshipType, personalNotes);
        if (updateRelationshipResult.IsFailure)
            return updateRelationshipResult.Error;

        var notificationResult = ChangeNotifications(
            notifyOnDeathAnniversary,
            notifyOnBirthAnniversary);

        if (notificationResult.IsFailure)
            return notificationResult.Error;

        Status = TrackStatus.Active;
        return UnitResult.Success<Error>();
    }

    public bool IsActive() => Status == TrackStatus.Active;
    public bool IsMuted() => Status == TrackStatus.Muted;
    public bool IsArchived() => Status == TrackStatus.Archived;

    public bool HasNotificationsEnabled() =>
        NotifyOnDeathAnniversary || NotifyOnBirthAnniversary;

    private static Result<string?, Error> NormalizePersonalNotes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Success<string?, Error>(null);

        var normalized = value.Trim();

        if (normalized.Length > MaxPersonalNotesLength)
            return Errors.Tracking.PersonalNotesTooLong(MaxPersonalNotesLength);

        return Result.Success<string?, Error>(normalized);
    }
}