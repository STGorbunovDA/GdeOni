using GdeOni.Domain.Aggregates.Events;

namespace GdeOni.Application.Abstractions.Persistence;

public interface ICustomEventRepository
{
    Task Add(CustomEvent customEvent, CancellationToken cancellationToken);

    /// <summary>Событие по id, только если принадлежит пользователю (иначе null).</summary>
    Task<CustomEvent?> GetByIdForUser(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<List<CustomEvent>> ListForUser(Guid userId, CancellationToken cancellationToken);

    void Delete(CustomEvent customEvent);

    Task Save(CancellationToken cancellationToken);
}
