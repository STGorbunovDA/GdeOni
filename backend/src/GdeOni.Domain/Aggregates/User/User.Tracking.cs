using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.User;

// Partial-split от User.cs (god-class). Bounded context "отслеживание
// умерших": подписка юзера на карточку, изменение статуса (Active/Muted/
// Archived), массовый remove админом и т.п.
public sealed partial class User
{
    public Result<TrackedDeceased, Error> TrackDeceased(
        Guid deceasedId,
        RelationshipType relationshipType,
        string? personalNotes = null,
        bool notifyOnDeathAnniversary = false,
        bool notifyOnBirthAnniversary = false)
    {
        var existingTracking = _trackedDeceasedItems
            .FirstOrDefault(x => x.DeceasedId == deceasedId);

        if (existingTracking is not null)
        {
            var reactivateResult = existingTracking.Reactivate(
                relationshipType,
                personalNotes,
                notifyOnDeathAnniversary,
                notifyOnBirthAnniversary);

            if (reactivateResult.IsFailure)
                return reactivateResult.Error;

            Touch();
            return Result.Success<TrackedDeceased, Error>(existingTracking);
        }

        var trackedResult = TrackedDeceased.Create(
            deceasedId,
            relationshipType,
            personalNotes,
            notifyOnDeathAnniversary,
            notifyOnBirthAnniversary);

        if (trackedResult.IsFailure)
            return trackedResult.Error;

        _trackedDeceasedItems.Add(trackedResult.Value);
        Touch();
        return Result.Success<TrackedDeceased, Error>(trackedResult.Value);
    }

    public Result<TrackStatus, Error> GetTrackingStatus(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        return Result.Success<TrackStatus, Error>(tracked.Status);
    }

    public TrackedDeceased? GetTracking(Guid deceasedId) =>
        _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);

    public UnitResult<Error> ChangeTrackingStatus(Guid deceasedId, TrackStatus status)
    {
        var result = status switch
        {
            TrackStatus.Active => ActivateTracking(deceasedId),
            TrackStatus.Muted => MuteTracking(deceasedId),
            TrackStatus.Archived => StopTracking(deceasedId),
            _ => Errors.Tracking.TrackStatusTypeInvalid()
        };

        if (result.IsSuccess)
            Touch();

        return result;
    }

    public UnitResult<Error> UpdateTracking(
        Guid deceasedId,
        RelationshipType relationshipType,
        string? personalNotes,
        bool notifyOnDeathAnniversary,
        bool notifyOnBirthAnniversary)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        var relationResult = tracked.UpdateRelationship(relationshipType, personalNotes);
        if (relationResult.IsFailure)
            return relationResult.Error;

        var notificationsResult = tracked.ChangeNotifications(
            notifyOnDeathAnniversary,
            notifyOnBirthAnniversary);

        if (notificationsResult.IsFailure)
            return notificationsResult;

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RemoveTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        _trackedDeceasedItems.Remove(tracked);
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Снимает все отслеживания юзера разом. Возвращает количество
    /// удалённых записей. Используется админом для bulk-операции.
    /// </summary>
    public int RemoveAllTracking()
    {
        var count = _trackedDeceasedItems.Count;
        if (count == 0) return 0;
        _trackedDeceasedItems.Clear();
        Touch();
        return count;
    }

    private UnitResult<Error> StopTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems
            .FirstOrDefault(x => x.DeceasedId == deceasedId && x.Status != TrackStatus.Archived);

        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        return tracked.Archive();
    }

    private UnitResult<Error> MuteTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        return tracked.Mute();
    }

    private UnitResult<Error> ActivateTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        return tracked.Activate();
    }
}
