namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaById.Model;

public sealed record GetMediaByIdQuery(Guid DeceasedId, Guid MediaId);
