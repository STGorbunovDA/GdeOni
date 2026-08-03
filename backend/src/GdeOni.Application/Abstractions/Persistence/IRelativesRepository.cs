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

public interface IRelativesRepository
{
    /// <summary>
    /// Находит «родственников» для пользователя: по каждой его АКТИВНО
    /// отслеживаемой карточке — других активных отслеживающих со связывающей
    /// связью (RelativeRelationships.Connectable), у кого включено согласие
    /// (AllowRelativeConnections) и кто не заблокирован. Считается вживую.
    /// </summary>
    Task<List<RelativeMatch>> GetRelativesForUser(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Можно ли viewer'у написать target'у по карточке deceasedId: оба активно
    /// её отслеживают, у target связывающая связь, включено согласие и он не
    /// заблокирован. Используется при старте диалога.
    /// </summary>
    Task<bool> IsRelative(
        Guid viewerId, Guid targetUserId, Guid deceasedId, CancellationToken cancellationToken);
}
