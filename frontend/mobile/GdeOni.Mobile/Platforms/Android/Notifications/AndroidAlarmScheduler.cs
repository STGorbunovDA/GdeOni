using Android.App;
using Android.Content;
using Android.OS;
using GdeOni.Mobile.Shared.Notifications;

namespace GdeOni.Mobile.Platforms.Android.Notifications;

/// <summary>
/// E23. Android-реализация <see cref="ILocalNotificationScheduler"/>
/// через AlarmManager + BroadcastReceiver. Выбор в пользу AlarmManager
/// (а не WorkManager как в исходном плане): меньше внешних зависимостей,
/// поддержка точных дат без обвязки с WorkRequest XML/Java interop.
/// Trade-off — AlarmManager не переживает reboot без BOOT_COMPLETED
/// receiver'а; ребут раз в год маловероятен (годовщина один раз в год),
/// но если станет проблемой — добавим BootReceiver + sync через бэк.
/// </summary>
public sealed class AndroidAlarmScheduler : ILocalNotificationScheduler
{
    private const int NotificationHourLocal = 9; // 09:00 локального времени

    public Task ScheduleAnniversaryAsync(
        AnniversaryReminder reminder,
        CancellationToken cancellationToken = default)
    {
        ReplaceAlarmStatic(global::Android.App.Application.Context, reminder);
        return Task.CompletedTask;
    }

    public Task CancelAsync(Guid deceasedId, AnniversaryKind kind, CancellationToken cancellationToken = default)
    {
        var context = global::Android.App.Application.Context;
        var manager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (manager is null) return Task.CompletedTask;

        var pending = BuildPendingIntent(context, deceasedId, kind, intent: null, flags: PendingIntentFlags.NoCreate | PendingIntentFlags.Immutable);
        if (pending is not null)
        {
            manager.Cancel(pending);
            pending.Cancel();
        }
        return Task.CompletedTask;
    }

    public async Task CancelAllForDeceasedAsync(Guid deceasedId, CancellationToken cancellationToken = default)
    {
        await CancelAsync(deceasedId, AnniversaryKind.Birth, cancellationToken);
        await CancelAsync(deceasedId, AnniversaryKind.Death, cancellationToken);
    }

    public async Task<bool> EnsureNotificationPermissionAsync()
    {
        // Android 13+ (API 33) требует runtime POST_NOTIFICATIONS.
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu) return true;

        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status == PermissionStatus.Granted) return true;

        status = await Permissions.RequestAsync<Permissions.PostNotifications>();
        return status == PermissionStatus.Granted;
    }

    /// <summary>
    /// Static helper — используется и из инстанс-метода, и из
    /// <see cref="AnniversaryAlarmReceiver"/> для self-rescheduling
    /// (receiver не знает про DI).
    /// </summary>
    /// <param name="strictlyAfterToday">
    /// true — self-reschedule после срабатывания alarm'а: следующая годовщина
    /// должна быть СТРОГО ПОСЛЕ сегодня. Иначе бесконечный цикл уведомлений
    /// (alarm выстрелил сегодня → reschedule на сегодня → снова выстрел).
    /// false (default) — первичный scheduling: если eventDate=сегодня и
    /// 09:00 ещё не наступило, alarm выстрелит сегодня.
    /// </param>
    internal static void ReplaceAlarmStatic(Context context, AnniversaryReminder reminder, bool strictlyAfterToday = false)
    {
        var manager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (manager is null) return;

        var today = DateOnly.FromDateTime(DateTime.Now);
        var nextAnniversary = strictlyAfterToday
            ? AnniversaryDateCalculator.NextAnniversaryAfter(reminder.EventDate, today)
            : AnniversaryDateCalculator.NextAnniversary(reminder.EventDate, today);

        // Триггерим в 09:00 местного — днем юзер скорее активен.
        var triggerLocal = new DateTime(
            nextAnniversary.Year, nextAnniversary.Month, nextAnniversary.Day,
            NotificationHourLocal, 0, 0, DateTimeKind.Local);

        var triggerMillis = new DateTimeOffset(triggerLocal).ToUnixTimeMilliseconds();

        var intent = new Intent(context, typeof(AnniversaryAlarmReceiver));
        intent.PutExtra(AnniversaryAlarmReceiver.ExtraDeceasedId, reminder.DeceasedId.ToString());
        intent.PutExtra(AnniversaryAlarmReceiver.ExtraFullName, reminder.FullName);
        intent.PutExtra(AnniversaryAlarmReceiver.ExtraKind, (int)reminder.Kind);
        intent.PutExtra(AnniversaryAlarmReceiver.ExtraEventDateIso, reminder.EventDate.ToString("yyyy-MM-dd"));

        var pending = BuildPendingIntent(
            context, reminder.DeceasedId, reminder.Kind, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        if (pending is null) return;

        // SetExactAndAllowWhileIdle — Doze-mode не сдвинет тригер.
        // Альтернатива SetAlarmClock даёт ещё более жёсткое поведение,
        // но требует UI-индикатора будильника в системном баре, что
        // тут лишнее.
        manager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerMillis, pending);
    }

    /// <summary>
    /// Стабильный requestCode = hash(deceasedId + kind). PendingIntent
    /// с одинаковым requestCode перезаписывается → idempotent
    /// re-scheduling.
    /// </summary>
    private static PendingIntent? BuildPendingIntent(
        Context context,
        Guid deceasedId,
        AnniversaryKind kind,
        Intent? intent,
        PendingIntentFlags flags)
    {
        var requestCode = HashCode.Combine(deceasedId, (int)kind);
        // GetBroadcast с null-intent и NoCreate возвращает null если
        // alarm не был зарегистрирован — используем для Cancel-проверки.
        return PendingIntent.GetBroadcast(
            context,
            requestCode,
            intent ?? new Intent(context, typeof(AnniversaryAlarmReceiver)),
            flags);
    }
}
