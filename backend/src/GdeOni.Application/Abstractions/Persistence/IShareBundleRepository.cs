using GdeOni.Domain.Aggregates.Sharing;

namespace GdeOni.Application.Abstractions.Persistence;

/// <summary>
/// D46. Хранилище подборок «поделиться». Контракт узкий: создать, найти по
/// коду, сохранить. Чистка протухших — отдельная забота (см. D46 в плане).
/// </summary>
public interface IShareBundleRepository
{
    Task Add(ShareBundle bundle, CancellationToken cancellationToken);

    /// <summary>Возвращает подборку по коду или null. Срок жизни проверяет
    /// уже сам агрегат/use case (<see cref="ShareBundle.IsExpired"/>).</summary>
    Task<ShareBundle?> GetByCode(string code, CancellationToken cancellationToken);

    /// <summary>True, если такой код уже занят — для проверки перед вставкой
    /// (коллизии практически не бывает, но unique-индекс + предпроверка
    /// закрывают её начисто).</summary>
    Task<bool> ExistsByCode(string code, CancellationToken cancellationToken);

    Task Save(CancellationToken cancellationToken);
}
