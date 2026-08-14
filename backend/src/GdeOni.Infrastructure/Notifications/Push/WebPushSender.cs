using System.Text.Json;
using GdeOni.Application.Abstractions.Notifications;
using GdeOni.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace GdeOni.Infrastructure.Notifications.Push;

/// <summary>
/// Отправка Web Push через VAPID. Payload — маленький JSON, который читает
/// service worker (см. public/sw.js): title/body/link.
///
/// Протухшие подписки чистим сами: push-сервис отвечает 404/410, когда
/// пользователь удалил PWA или отозвал разрешение. Без этого таблица копила
/// бы мёртвые адреса и мы дёргали бы их вечно.
///
/// Всё best-effort: исключения наружу не выпускаем — уведомление не должно
/// ронять операцию, из которой его шлют.
/// </summary>
public sealed class WebPushSender(
    AppDbContext dbContext,
    IOptions<WebPushOptions> options,
    TimeProvider timeProvider,
    ILogger<WebPushSender> logger) : IPushSender
{
    private readonly WebPushOptions _options = options.Value;

    public Task SendToUserAsync(
        Guid userId,
        string title,
        string? body,
        string? link,
        CancellationToken cancellationToken)
        => SendToUsersAsync(new[] { userId }, title, body, link, cancellationToken);

    public async Task SendToUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        string title,
        string? body,
        string? link,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return;

        try
        {
            var ids = userIds as Guid[] ?? userIds.ToArray();

            var subscriptions = await dbContext.PushSubscriptions
                .Where(x => ids.Contains(x.UserId))
                .ToListAsync(cancellationToken);

            if (subscriptions.Count == 0)
                return;

            var payload = JsonSerializer.Serialize(new
            {
                title,
                body,
                link,
            });

            var client = new WebPushClient();
            var vapid = new VapidDetails(
                _options.Subject,
                _options.PublicKey,
                _options.PrivateKey);

            var dead = new List<PushSubscriptionRecord>();
            var now = timeProvider.GetUtcNow().UtcDateTime;

            foreach (var record in subscriptions)
            {
                var subscription = new WebPush.PushSubscription(
                    record.Endpoint, record.P256dh, record.Auth);

                try
                {
                    await client.SendNotificationAsync(
                        subscription, payload, vapid, cancellationToken);
                    record.LastSuccessAtUtc = now;
                }
                catch (WebPushException ex) when (
                    ex.StatusCode == System.Net.HttpStatusCode.NotFound
                    || ex.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    // Подписка мертва (PWA удалили / разрешение отозвали).
                    dead.Add(record);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Не удалось отправить push на {Endpoint}.",
                        record.Endpoint);
                }
            }

            if (dead.Count > 0)
                dbContext.PushSubscriptions.RemoveRange(dead);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Сбой рассылки push-уведомлений.");
        }
    }
}

/// <summary>
/// Заглушка, когда VAPID-ключи не сконфигурированы: приложение работает как
/// раньше (письма + «колокольчик»), push просто не шлётся. Зеркалит
/// NoOpEmailSender.
/// </summary>
public sealed class NoOpPushSender : IPushSender
{
    public Task SendToUserAsync(
        Guid userId, string title, string? body, string? link, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task SendToUsersAsync(
        IReadOnlyCollection<Guid> userIds, string title, string? body, string? link, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
