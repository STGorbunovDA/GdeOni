using GdeOni.Domain.Aggregates.Notifications;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Notifications;

/// <summary>
/// Сервис доставки внутрисайтовых уведомлений. Зовётся из доменных use case'ов
/// ПОСЛЕ их основного Save — best-effort: собственные ошибки глотает и логирует,
/// чтобы сбой уведомления никогда не ронял основную операцию (создание
/// обращения/жалобы, ответ админа и т.п.).
/// </summary>
public interface INotificationService
{
    /// <summary>Уведомить одного пользователя.</summary>
    Task NotifyUserAsync(
        Guid recipientUserId,
        NotificationKind kind,
        string title,
        string? body,
        string? link,
        CancellationToken cancellationToken);

    /// <summary>
    /// Уведомить всех пользователей с указанными ролями (например всех
    /// SuperAdmin — о новом обращении). На каждого — своя запись.
    /// </summary>
    Task NotifyRolesAsync(
        IReadOnlyCollection<UserRole> roles,
        NotificationKind kind,
        string title,
        string? body,
        string? link,
        CancellationToken cancellationToken);
}
