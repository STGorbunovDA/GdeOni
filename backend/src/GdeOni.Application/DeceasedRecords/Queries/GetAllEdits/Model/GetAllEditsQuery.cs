namespace GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.Model;

/// <summary>
/// D24/F17.9. Все правки карточек умерших по системе. Только админ.
/// Фильтры (все опциональные):
///   DeceasedId — правки одной карточки;
///   EditorUserId — правки одного редактора;
///   EditedFromUtc/EditedToUtc — диапазон дат (включительно по дням).
/// </summary>
public sealed record GetAllEditsQuery(
    int Page,
    int PageSize,
    Guid? DeceasedId = null,
    Guid? EditorUserId = null,
    DateTime? EditedFromUtc = null,
    DateTime? EditedToUtc = null);
