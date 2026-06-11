using Android.App;
using Android.Content;
using GdeOni.Mobile.Services.Notifications;

namespace GdeOni.Mobile.Platforms.Android.Notifications;

/// <summary>
/// E23 C.3. После <c>BOOT_COMPLETED</c> восстанавливаем annivers-alarms.
/// AlarmManager НЕ переживает reboot — без этого receiver'а юзер не получит
/// уведомлений до следующего открытия приложения (когда сработает sync
/// в AppShell.OnAppearing).
///
/// Trade-off: receiver работает на main-thread процесса и не должен
/// блокировать; <see cref="AnniversariesSyncService.SyncAsync"/> делает
/// HTTP-запрос, поэтому запускаем через Task.Run + goAsync чтобы Android
/// не убил процесс посреди работы.
/// </summary>
[BroadcastReceiver(
    Name = "com.gdeoni.mobile.BootRestoreReceiver",
    Enabled = true,
    Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted, "android.intent.action.QUICKBOOT_POWERON" })]
public sealed class BootRestoreReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null) return;
        if (intent?.Action != Intent.ActionBootCompleted &&
            intent?.Action != "android.intent.action.QUICKBOOT_POWERON")
            return;

        // goAsync даёт Android понять что мы ещё работаем — иначе процесс
        // прибивают как только OnReceive вернёт control.
        var pending = GoAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                // Сервисы доступны через IPlatformApplication. Если MAUI
                // ещё не инициализирован (теоретически возможно сразу после
                // reboot) — пропускаем; следующий sync через AppShell
                // подхватит.
                var sync = IPlatformApplication.Current?.Services?
                    .GetService(typeof(AnniversariesSyncService)) as AnniversariesSyncService;
                if (sync is null) return;

                await sync.SyncAsync();
            }
            catch
            {
                // Best-effort. Если sync упал — alarms восстановит ближайший
                // запуск приложения через AppShell.OnAppearing.
            }
            finally
            {
                pending.Finish();
            }
        });
    }
}
