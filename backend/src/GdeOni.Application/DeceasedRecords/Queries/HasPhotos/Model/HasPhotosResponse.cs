namespace GdeOni.Application.DeceasedRecords.Queries.HasPhotos.Model;

public sealed record HasPhotosResponse(
    Guid DeceasedId,
    bool HasPhotos);