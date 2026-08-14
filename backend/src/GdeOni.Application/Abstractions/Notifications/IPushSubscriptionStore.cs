namespace GdeOni.Application.Abstractions.Notifications;

/// <summary>
/// Подписка браузера на push. Endpoint + два ключа выдаёт сам браузер
/// (PushManager.subscribe), сервер их только хранит и использует при отправке.
/// </summary>
public sealed record PushSubscriptionData(
    string Endpoint,
    string P256dh,
    string Auth);

/// <summary>
/// Хранилище push-подписок. У одного пользователя их может быть несколько —
/// по одной на каждое устройство/браузер.
/// </summary>
public interface IPushSubscriptionStore
{
    /// <summary>
    /// Сохранить подписку. Идемпотентно по endpoint: браузер может прислать
    /// ту же подписку повторно (переустановка PWA, повторный вызов subscribe),
    /// и плодить дубли не надо — иначе один пуш придёт несколько раз.
    /// </summary>
    Task SaveAsync(
        Guid userId,
        PushSubscriptionData subscription,
        CancellationToken cancellationToken);

    /// <summary>Удалить подписку по endpoint (юзер выключил уведомления).</summary>
    Task RemoveAsync(string endpoint, CancellationToken cancellationToken);

    /// <summary>Все подписки пользователя — на них и рассылаем.</summary>
    Task<List<PushSubscriptionData>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>Есть ли у пользователя хоть одна подписка (для UI-статуса).</summary>
    Task<bool> HasAnyAsync(Guid userId, CancellationToken cancellationToken);
}
