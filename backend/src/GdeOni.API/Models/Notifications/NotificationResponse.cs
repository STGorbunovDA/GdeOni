using GdeOni.Domain.Aggregates.Notifications;

namespace GdeOni.API.Models.Notifications;

/// <summary>
/// Уведомление для клиента («колокольчик»). Kind отдаётся строкой (имя enum) —
/// клиент выбирает по нему иконку; Link — куда вести по клику (может быть null).
/// </summary>
public sealed record NotificationResponse(
    Guid Id,
    string Kind,
    string Title,
    string? Body,
    string? Link,
    bool IsRead,
    DateTime CreatedAtUtc)
{
    public static NotificationResponse From(Notification n) => new(
        n.Id,
        n.Kind.ToString(),
        n.Title,
        n.Body,
        n.Link,
        n.IsRead,
        n.CreatedAtUtc);
}

/// <summary>Число непрочитанных уведомлений — для бейджа на колокольчике.</summary>
public sealed record UnreadCountResponse(int Count);
