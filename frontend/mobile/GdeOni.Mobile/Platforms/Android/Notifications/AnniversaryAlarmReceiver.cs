using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using GdeOni.Mobile.Shared.Notifications;

namespace GdeOni.Mobile.Platforms.Android.Notifications;

/// <summary>
/// E23. BroadcastReceiver, который Android вызывает в момент срабатывания
/// AlarmManager. Показывает notification и сразу же планирует следующую
/// годовщину через ровно год (self-rescheduling — план E23 предписывает
/// точные даты, а не PeriodicWorkRequest-интервалы).
/// </summary>
[BroadcastReceiver(
    Name = "com.gdeoni.mobile.AnniversaryAlarmReceiver",
    Exported = false)]
public sealed class AnniversaryAlarmReceiver : BroadcastReceiver
{
    public const string ChannelId = "gdeoni.anniversaries";
    public const string ExtraDeceasedId = "deceasedId";
    public const string ExtraFullName = "fullName";
    public const string ExtraKind = "kind";
    public const string ExtraEventDateIso = "eventDateIso";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;

        var deceasedId = intent.GetStringExtra(ExtraDeceasedId);
        var fullName = intent.GetStringExtra(ExtraFullName);
        var kindRaw = intent.GetIntExtra(ExtraKind, -1);
        var eventDateIso = intent.GetStringExtra(ExtraEventDateIso);

        if (string.IsNullOrWhiteSpace(deceasedId) ||
            string.IsNullOrWhiteSpace(fullName) ||
            kindRaw < 0 ||
            !Enum.IsDefined(typeof(AnniversaryKind), kindRaw))
        {
            return;
        }

        var kind = (AnniversaryKind)kindRaw;
        EnsureChannelExists(context);
        ShowNotification(context, deceasedId, fullName!, kind);

        // Self-reschedule СТРОГО на следующий год: alarm только что выстрелил
        // сегодня, без strictlyAfterToday получили бы бесконечный цикл
        // (NextAnniversary вернёт сегодня → alarm в прошлом → SetExactAndAllowWhileIdle
        // выстрелит сразу → receiver снова сюда → ...).
        if (DateOnly.TryParseExact(eventDateIso, "yyyy-MM-dd", out var eventDate))
        {
            AndroidAlarmScheduler.ReplaceAlarmStatic(
                context,
                new AnniversaryReminder(
                    Guid.Parse(deceasedId!),
                    fullName!,
                    eventDate,
                    kind),
                strictlyAfterToday: true);
        }
    }

    private static void EnsureChannelExists(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null) return;

        if (manager.GetNotificationChannel(ChannelId) is not null) return;

        var channel = new NotificationChannel(
            ChannelId,
            "Годовщины",
            NotificationImportance.Default)
        {
            Description = "Напоминания о датах рождения и смерти отслеживаемых людей.",
        };
        manager.CreateNotificationChannel(channel);
    }

    private static void ShowNotification(Context context, string deceasedId, string fullName, AnniversaryKind kind)
    {
        var (title, body) = kind switch
        {
            AnniversaryKind.Birth => ("Годовщина рождения", $"Сегодня день рождения {fullName}."),
            AnniversaryKind.Death => ("Годовщина смерти", $"Сегодня годовщина {fullName}."),
            _ => ("Напоминание", fullName),
        };

        // Tap на notification поднимает приложение. Углубление до
        // конкретной карточки умершего (deep link на DeceasedDetailsPage)
        // — отдельная задача (E23 follow-up).
        var openIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        var pending = openIntent is null
            ? null
            : PendingIntent.GetActivity(
                context,
                deceasedId.GetHashCode(),
                openIntent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var notif = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetAutoCancel(true)
            .SetContentIntent(pending)
            .Build();

        var manager = NotificationManagerCompat.From(context);
        // Tag = deceasedId-kind гарантирует что повторная годовщина того
        // же типа перепишет старое уведомление, а не плодит несколько.
        manager.Notify($"{deceasedId}-{(int)kind}", 0, notif);
    }
}
