namespace GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.Model;

public sealed record GetDeceasedEditsQuery(
    Guid DeceasedId,
    int Page,
    int PageSize);
