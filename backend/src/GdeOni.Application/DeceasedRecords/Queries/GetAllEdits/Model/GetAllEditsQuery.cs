namespace GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.Model;

/// <summary>
/// D24/F17.9. Все правки карточек умерших по системе. Только админ.
/// </summary>
public sealed record GetAllEditsQuery(int Page, int PageSize);
