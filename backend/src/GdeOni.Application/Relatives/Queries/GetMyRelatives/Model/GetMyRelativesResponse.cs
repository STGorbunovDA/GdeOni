namespace GdeOni.Application.Relatives.Queries.GetMyRelatives.Model;

/// <summary>
/// Функция «Родственники». Одна строка вкладки: по карточке [DeceasedFullName]
/// есть родственник [RelativeUserName], кем приходится — [RelationshipType].
/// Почты нет — переписка внутренняя (Фаза 3).
/// </summary>
public sealed record MyRelativeItemResponse(
    Guid DeceasedId,
    string DeceasedFullName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    Guid RelativeUserId,
    string RelativeUserName,
    string RelationshipType);

public sealed record GetMyRelativesResponse(IReadOnlyList<MyRelativeItemResponse> Items);
