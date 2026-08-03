using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Persistence;

/// <summary>
/// Функция «Родственники». Одно совпадение: по карточке умершего, которую
/// отслеживает текущий пользователь, есть ДРУГОЙ пользователь, который тоже
/// её отслеживает (активно), указал связывающий тип связи и не отключил
/// согласие. Email не входит — переписка внутренняя (Фаза 3).
/// </summary>
public sealed record RelativeMatch(
    Guid DeceasedId,
    string DeceasedFullName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    Guid RelativeUserId,
    string RelativeUserName,
    RelationshipType RelationshipType);

/// <summary>
/// Фаза 4. «Новый» родственник для владельца — обнаруженный ночным джобом и
/// ещё не просмотренный (is_new). Отдаётся уже перепроверенным по текущему
/// состоянию (связь всё ещё связывающая, согласие включено, не заблокирован).
/// </summary>
public sealed record NewRelativeItem(
    Guid DeceasedId,
    string DeceasedFullName,
    Guid RelativeUserId,
    string RelativeUserName,
    RelationshipType RelationshipType,
    DateTime DiscoveredAtUtc);

public interface IRelativesRepository
{
    /// <summary>
    /// Находит «родственников» для пользователя: по каждой его АКТИВНО
    /// отслеживаемой карточке — ВСЕХ других активных отслеживающих (связь
    /// любая), у кого включено согласие (AllowRelativeConnections) и кто не
    /// заблокирован. Фильтр по связи — на клиенте. Считается вживую.
    /// </summary>
    Task<List<RelativeMatch>> GetRelativesForUser(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Можно ли viewer'у написать target'у по карточке deceasedId: оба активно
    /// её отслеживают, у target включено согласие и он не заблокирован.
    /// Используется при старте диалога.
    /// </summary>
    Task<bool> IsRelative(
        Guid viewerId, Guid targetUserId, Guid deceasedId, CancellationToken cancellationToken);

    /// <summary>
    /// Фаза 4. «Новые» родственники владельца (обнаружены джобом, is_new и
    /// всё ещё валидны по текущему состоянию) — для попапа «События» и бейджа.
    /// </summary>
    Task<List<NewRelativeItem>> GetNewRelatives(Guid ownerId, CancellationToken cancellationToken);

    /// <summary>
    /// Фаза 4. Отметить всех «новых» родственников владельца просмотренными
    /// (сбрасывает is_new). Immediate-запись (ExecuteUpdate), минуя Save —
    /// вызывается при заходе на вкладку «Родственники».
    /// </summary>
    Task MarkRelativesSeen(Guid ownerId, CancellationToken cancellationToken);
}
