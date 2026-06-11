namespace GdeOni.Domain.Shared;

/// <summary>
/// D16. Состояние подписки пользователя.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Подписка не активна и Trial не стартовал.
    /// Initial state до первого вызова <c>User.StartTrial</c> в
    /// <c>RegisterUserUseCase</c>. После регистрации пользователь
    /// сразу переходит в <see cref="Trial"/>.
    /// </summary>
    None = 0,

    /// <summary>
    /// Пробный период (30 дней по дефолту). С точки зрения гейта
    /// эквивалентен <see cref="Active"/>: пользователь получает
    /// полный доступ.
    /// </summary>
    Trial = 1,

    /// <summary>
    /// Платёж в YooKassa создан, но webhook о подтверждении ещё
    /// не пришёл. Доступ зависит от <see cref="HasActiveSubscription"/>:
    /// если до перевода в PendingPayment был Trial с ExpiresAtUtc &gt; now —
    /// доступ остаётся, иначе блок.
    /// </summary>
    PendingPayment = 2,

    /// <summary>
    /// Оплачена и действует.
    /// </summary>
    Active = 3,

    /// <summary>
    /// Отменена пользователем. ExpiresAtUtc сохраняется — paid-period
    /// дорабатывает до конца, после чего HasActiveSubscription
    /// возвращает false.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Истёкшая подписка после grace-периода. Использования сейчас не
    /// требует (HasActiveSubscription сам считает по ExpiresAtUtc), но
    /// оставлено для возможной фоновой "expired-marking" job'ы в будущем.
    /// </summary>
    Expired = 5,
}
