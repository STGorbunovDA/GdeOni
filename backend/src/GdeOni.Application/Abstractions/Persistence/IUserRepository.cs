using GdeOni.Application.Users.Queries.GetAll.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

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
    /// D43. Поиск пользователя по хешу токена восстановления пароля.
    /// Возвращает null, если такого токена нет — срок действия проверяет
    /// уже сам агрегат в <c>ResetPasswordByToken</c>, чтобы инвариант
    /// жил в домене, а не размазывался по запросу.
    /// </summary>
    Task<User?> GetByPasswordResetTokenHash(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// D45. Поиск пользователя по хешу токена подтверждения email.
    /// Возвращает null, если такого токена нет — срок действия и
    /// одноразовость проверяет уже сам агрегат в <c>ConfirmEmailByToken</c>.
    /// </summary>
    Task<User?> GetByEmailConfirmationTokenHash(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Лёгкий lookup только email'а по id. Нужен для отображения "кто
    /// заблокировал" в GetUserById — поднимать второй User entity
    /// ради одного string'а избыточно.
    /// </summary>
    Task<string?> GetEmailById(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Batch-выборка отображаемых имён авторов: возвращает
    /// <c>FullName ?? UserName</c> для каждого id из списка. Используется
    /// в DeceasedDetails чтобы показывать "автор: Иван Петров" под
    /// каждым воспоминанием без N+1 запросов. Несуществующие id
    /// (юзер удалил аккаунт) в словарь не попадают — UI отрисует
    /// "Аноним".
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIds(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);

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
    /// не должны их видеть. <paramref name="excludeUserId"/> — id юзера
    /// которого надо исключить (типичный случай: текущий админ не должен
    /// видеть сам себя в списке).
    /// </summary>
    Task<(List<(User User, int TrackingCount)> Items, int TotalCount)> GetPaged(
        GetAllUsersQuery query,
        bool includeSuperAdmins,
        Guid? excludeUserId,
        CancellationToken cancellationToken);
    Task<bool> ExistsById(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Id всех НЕзаблокированных пользователей с указанными ролями — для
    /// адресной рассылки уведомлений (напр. всем SuperAdmin о новом обращении).
    /// </summary>
    Task<List<Guid>> GetIdsByRoles(
        IReadOnlyCollection<UserRole> roles,
        CancellationToken cancellationToken);

    /// <summary>
    /// Массовая выдача комплиментарного доступа ВСЕМ пользователям до
    /// <paramref name="untilUtc"/>. Только ПРОДЛЕВАЕТ: строки, где комплимент
    /// уже выдан на более поздний срок, не трогаются. ExecuteUpdate напрямую
    /// (минуя Save), как RefreshTokenRepository.RevokeAllForUser — админская
    /// bulk-операция. Возвращает число затронутых пользователей.
    /// </summary>
    Task<int> GrantComplimentaryAccessToAll(
        DateTime untilUtc,
        Guid grantedByAdminId,
        string? note,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// F40. Публичный счётчик: всего зарегистрированных пользователей.
    /// Для стартовой страницы (<c>GET /api/app/stats</c>) — простой COUNT(*).
    /// </summary>
    Task<int> CountAllAsync(CancellationToken cancellationToken);
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