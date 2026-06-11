namespace GdeOni.Mobile.Shared.Notifications;

/// <summary>
/// E23. Кросс-платформенная абстракция локальных уведомлений.
/// Android-реализация через AlarmManager + NotificationManager
/// (см. <c>Platforms/Android/Notifications/AndroidAlarmScheduler.cs</c>).
/// iOS — заготовка-stub, реальную реализацию через
/// <c>UNUserNotificationCenter</c> добавим когда дойдём до iOS (H4).
/// </summary>
public interface ILocalNotificationScheduler
{
    /// <summary>
    /// Запланировать (или перепланировать) годовщину. Идемпотентно по
    /// (DeceasedId, Kind): повторный вызов с теми же параметрами
    /// перезаписывает существующий alarm.
    /// </summary>
    Task ScheduleAnniversaryAsync(AnniversaryReminder reminder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отменить конкретную годовщину (например, юзер выключил тоггл
    /// "уведомлять о дне рождения").
    /// </summary>
    Task CancelAsync(Guid deceasedId, AnniversaryKind kind, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отменить оба напоминания у конкретного умершего (например, юзер
    /// отписался полностью).
    /// </summary>
    Task CancelAllForDeceasedAsync(Guid deceasedId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Запросить у пользователя permission на отправку уведомлений
    /// (Android 13+ — POST_NOTIFICATIONS, на более старых API всегда
    /// возвращает true). Вызывается контекстно — при первом включении
    /// тоггла anniversary, не на splash'е.
    /// </summary>
    Task<bool> EnsureNotificationPermissionAsync();
}
