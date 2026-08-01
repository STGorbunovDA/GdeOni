namespace GdeOni.Application.Sharing.Queries.GetShareBundle.Model;

/// <summary>
/// D46. Одна строка подборки для экрана получателя: только ФИО + даты +
/// место. Фото/биографию/воспоминания намеренно не отдаём (получатель
/// увидит их уже в отслеживании после «Добавить»).
/// </summary>
public sealed record ShareBundleItemResponse(
    Guid DeceasedId,
    string FullName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    string? Country,
    string? City,
    string? CemeteryName);
