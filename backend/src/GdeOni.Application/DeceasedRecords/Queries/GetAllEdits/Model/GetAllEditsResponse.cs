using GdeOni.Domain.Aggregates.DeceasedRecords;

namespace GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.Model;

public sealed record GetAllEditsResponse(
    IReadOnlyList<DeceasedEditWithCardItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record DeceasedEditWithCardItem(
    Guid Id,
    DateTime EditedAtUtc,
    Guid DeceasedId,
    string DeceasedFullName,
    Guid? EditedByUserId,
    string? EditedByEmail,
    string? EditedByDisplayName,
    DeceasedEditKind Kind,
    string ChangesJson);
