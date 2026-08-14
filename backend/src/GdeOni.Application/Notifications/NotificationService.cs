using GdeOni.Application.Abstractions.Notifications;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Notifications;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GdeOni.Application.Notifications;

/// <summary>
/// Реализация доставки уведомлений. Всё best-effort: любые исключения
/// глотаются и логируются — уведомление вторично, оно не должно ронять
/// основную операцию (тикет/жалоба уже сохранены к моменту вызова).
/// Сохраняет своей транзакцией (отдельный Save на том же scoped DbContext,
/// когда основной Save use case'а уже прошёл).
/// </summary>
public sealed class NotificationService(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    IPushSender pushSender,
    TimeProvider timeProvider,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task NotifyUserAsync(
        Guid recipientUserId,
        NotificationKind kind,
        string title,
        string? body,
        string? link,
        CancellationToken cancellationToken)
    {
        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var notification = Notification.Create(recipientUserId, kind, title, body, link, now);
            await notificationRepository.Add(notification, cancellationToken);
            await notificationRepository.Save(cancellationToken);

            // Тот же текст уходит push'ем на телефон — «колокольчик» человек
            // увидит только когда сам зайдёт.
            await pushSender.SendToUserAsync(
                recipientUserId, title, body, link, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Не удалось создать уведомление {Kind} пользователю {UserId}",
                kind, recipientUserId);
        }
    }

    public async Task NotifyRolesAsync(
        IReadOnlyCollection<UserRole> roles,
        NotificationKind kind,
        string title,
        string? body,
        string? link,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipients = await userRepository.GetIdsByRoles(roles, cancellationToken);
            if (recipients.Count == 0)
                return;

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var notifications = recipients
                .Select(id => Notification.Create(id, kind, title, body, link, now))
                .ToList();

            await notificationRepository.AddRange(notifications, cancellationToken);
            await notificationRepository.Save(cancellationToken);

            await pushSender.SendToUsersAsync(
                recipients, title, body, link, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Не удалось разослать уведомление {Kind} ролям {Roles}",
                kind, string.Join(",", roles));
        }
    }
}
