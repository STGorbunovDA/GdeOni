using GdeOni.Domain.Aggregates.DeceasedRecords;

namespace GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.Model;

public sealed record GetDeceasedEditsResponse(
    IReadOnlyList<DeceasedEditItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record DeceasedEditItem(
    Guid Id,
    DateTime EditedAtUtc,
    Guid? EditedByUserId,
    string? EditedByEmail,
    string? EditedByDisplayName,
    DeceasedEditKind Kind,
    string ChangesJson);
