namespace GdeOni.Application.DeceasedRecords.HasPhotos.Model;

public sealed record HasPhotosResponse(
    Guid DeceasedId,
    bool HasPhotos);