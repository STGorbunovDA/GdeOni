namespace GdeOni.Application.Abstractions.Notifications;

/// <summary>
/// Отправка browser-push (Web Push / PWA). Дополняет письма и «колокольчик»:
/// почту половина не читает, а пуш приходит на телефон сразу.
///
/// Всё best-effort: реализация глотает свои ошибки и логирует их — сбой
/// доставки не должен ронять операцию, из которой пуш отправляют.
/// </summary>
public interface IPushSender
{
    /// <summary>
    /// Отправить пуш на все устройства пользователя. <paramref name="link"/> —
    /// относительный путь, куда вести по клику (может быть null).
    /// </summary>
    Task SendToUserAsync(
        Guid userId,
        string title,
        string? body,
        string? link,
        CancellationToken cancellationToken);

    /// <summary>Батч на несколько получателей (фан-аут админам).</summary>
    Task SendToUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        string title,
        string? body,
        string? link,
        CancellationToken cancellationToken);
}
