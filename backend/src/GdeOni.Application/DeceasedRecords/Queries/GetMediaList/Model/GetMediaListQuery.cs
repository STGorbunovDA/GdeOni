using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaList.Model;

public sealed record GetMediaListQuery(
    Guid DeceasedId,
    MediaKind? Kind,
    ModerationStatus? ModerationStatus,
    int Page,
    int PageSize);
