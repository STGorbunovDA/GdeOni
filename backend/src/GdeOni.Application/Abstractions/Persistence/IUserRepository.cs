using GdeOni.Application.Users.Queries.GetAll.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetById(Guid userId, CancellationToken cancellationToken);
    Task<User?> GetByIdReadOnly(Guid userId, CancellationToken cancellationToken);
    Task<User?> GetByIdWithTrackingByDeceasedId(Guid userId, Guid deceasedId, CancellationToken cancellationToken);
    Task<(User User, int TrackingCount)?> GetByIdWithTrackingCount(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// User с полной коллекцией TrackedDeceasedItems — для bulk-операций
    /// (например, админский RemoveAllTracking).
    /// </summary>
    Task<User?> GetByIdWithAllTracking(Guid userId, CancellationToken cancellationToken);
    Task<User?> GetByEmail(string email, CancellationToken cancellationToken);

    /// <summary>
    /// D16. Поиск пользователя по <c>Subscription.LastPaymentId</c>.
    /// Используется <c>ProcessPaymentWebhookUseCase</c> чтобы найти,
    /// кого активировать после webhook YooKassa. Возвращает null
    /// если paymentId не известен (например, webhook от устаревшего
    /// или подделанного платежа).
    /// </summary>
    Task<User?> GetBySubscriptionPaymentId(string externalPaymentId, CancellationToken cancellationToken);
    /// <summary>
    /// Список юзеров с пагинацией. <paramref name="includeSuperAdmins"/>
    /// контролирует видимость SuperAdmin'ов: false — обычные админы
    /// не должны их видеть.
    /// </summary>
    Task<(List<(User User, int TrackingCount)> Items, int TotalCount)> GetPaged(
        GetAllUsersQuery query,
        bool includeSuperAdmins,
        CancellationToken cancellationToken);
    Task<bool> ExistsById(Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByUserName(string userName, CancellationToken cancellationToken);
    Task<bool> IsActivelyTracking(Guid userId, Guid deceasedId, CancellationToken cancellationToken);
    Task<(List<(TrackedDeceased Tracking, Deceased Deceased)> Items, int TotalCount)> GetMyTrackedDeceasedPaged(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    void Delete(User user);
    Task Add(User user, CancellationToken cancellationToken);
    Task Save(CancellationToken cancellationToken);
}