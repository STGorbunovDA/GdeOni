namespace GdeOni.Application.Sharing.Queries.GetShareBundle.Model;

/// <summary>
/// D46. Одна строка подборки для экрана получателя: только ФИО + даты +
/// место. Фото/биографию/воспоминания намеренно не отдаём (получатель
/// увидит их уже в отслеживании после «Добавить»).
///
/// <para>
/// TrackingStatus — статус этой карточки у ТЕКУЩЕГО получателя:
/// <c>null</c> — не отслеживает (будет добавлена), иначе строка статуса
/// (<c>Active</c>/<c>Muted</c>/<c>Archived</c>). Экран получателя по нему
/// показывает «уже в списке / в архиве» ещё до нажатия «Добавить», а импорт
/// такие карточки не трогает (см. D46 follow-up).
/// </para>
/// </summary>
public sealed record ShareBundleItemResponse(
    Guid DeceasedId,
    string FullName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    string? Country,
    string? City,
    string? CemeteryName,
    string? TrackingStatus);
