namespace GdeOni.Application.Users.Queries.GetById.Model;

public sealed class GetUserByIdResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string? FullName { get; init; }
    public string Role { get; init; } = null!;
    public DateTime RegisteredAtUtc { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public int TrackingCount { get; init; }

    /// <summary>
    /// Текущий статус подписки (None/Trial/PendingPayment/Active/
    /// Cancelled/Expired). Для админ-страницы детального вида юзера.
    /// </summary>
    public string SubscriptionStatus { get; init; } = null!;
    public DateTime? SubscriptionExpiresAtUtc { get; init; }
    public string? SubscriptionPlan { get; init; }

    /// <summary>
    /// D22. Имеет ли юзер активный complimentary access от админа
    /// (UntilUtc=null или > now).
    /// </summary>
    public bool HasComplimentaryAccess { get; init; }
    public DateTime? ComplimentaryAccessUntilUtc { get; init; }
    public string? ComplimentaryAccessNote { get; init; }

    /// <summary>
    /// F17.10. Поля блокировки. BlockedByUserEmail подтягивается join'ом
    /// в use case (репозитория для админ-сводки нет — раз на детальный
    /// экран допустимо).
    /// </summary>
    public bool IsBlocked { get; init; }
    public DateTime? BlockedAtUtc { get; init; }
    public Guid? BlockedByUserId { get; init; }
    public string? BlockedByUserEmail { get; init; }
    public string? BlockedReason { get; init; }
}