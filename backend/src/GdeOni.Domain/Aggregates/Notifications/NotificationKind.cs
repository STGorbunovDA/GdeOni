namespace GdeOni.Domain.Aggregates.Notifications;

/// <summary>
/// Тип уведомления — определяет иконку/смысл на клиенте. Значения храним
/// как int (HasConversion) — порядок фиксирован, дописывать только в конец.
/// </summary>
public enum NotificationKind
{
    /// <summary>Пользователь создал обращение → уведомляем администрацию.</summary>
    SupportTicketCreated = 1,

    /// <summary>Админ ответил в обращении → уведомляем автора обращения.</summary>
    SupportTicketReplied = 2,

    /// <summary>Пользователь подал жалобу на родственника → уведомляем администрацию.</summary>
    RelativeReportCreated = 3,

    /// <summary>Админ разобрал жалобу → уведомляем подавшего.</summary>
    RelativeReportResolved = 4,
}
