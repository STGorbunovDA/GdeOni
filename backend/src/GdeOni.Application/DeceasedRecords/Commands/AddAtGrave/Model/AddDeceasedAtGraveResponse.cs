namespace GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.Model;

public sealed record AddDeceasedAtGraveResponse(
    Guid DeceasedId,
    Guid TrackingId,
    string TrackingStatus);
