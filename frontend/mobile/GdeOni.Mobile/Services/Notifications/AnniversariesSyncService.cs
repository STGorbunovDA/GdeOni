using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Shared.Notifications;

namespace GdeOni.Mobile.Services.Notifications;

/// <summary>
/// E23 follow-up C.1. Восстанавливает локальные anniversary-alarms из
/// серверного состояния. Дёргается:
///   - после успешного логина (новый телефон / переустановка APK);
///   - на старте AppShell.OnAppearing если уже есть сессия (страховка
///     если предыдущий sync не отработал, дешевле чем хранить лишний флаг);
///   - после BOOT_COMPLETED (см. <c>BootRestoreReceiver</c>, C.3).
///
/// Логика: пройти всех Active-трекаемых, и для каждого — schedule alarms
/// тех тогглов, что включены. ReplaceAlarmStatic в AndroidAlarmScheduler
/// идемпотентен (PendingIntent с одинаковым requestCode перезаписывается),
/// поэтому повторный sync — безопасный no-op.
/// </summary>
public sealed class AnniversariesSyncService(
    ITrackedDeceasedApi trackedApi,
    ILocalNotificationScheduler notificationScheduler)
{
    private const int PageSize = 100;

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Permission запрашиваем ОДИН раз перед циклом — иначе на
            // каждой карточке всплывал бы system prompt. Если отказан —
            // просто выходим без планирования (юзер сам перезапросит
            // через тоггл в карточке).
            if (!await notificationScheduler.EnsureNotificationPermissionAsync())
                return;

            var page = 1;
            while (true)
            {
                var envelope = await trackedApi.GetListAsync(page, PageSize, cancellationToken);
                if (envelope.Result is null) return;

                foreach (var item in envelope.Result.Items)
                {
                    // Archived не трекаем — они не должны звенеть.
                    if (item.Status == TrackStatuses.Archived) continue;
                    await ScheduleForItemAsync(item, cancellationToken);
                }

                if (envelope.Result.Items.Count < PageSize) return;
                page++;
                // Защита от бесконечного цикла при странном backend-ответе.
                if (page > 50) return;
            }
        }
        catch
        {
            // Sync — best-effort. Ошибки не блокируют логин и не показываются
            // юзеру: в худшем случае alarms просто не восстановятся, юзер
            // увидит через ручной тоггл.
        }
    }

    private async Task ScheduleForItemAsync(TrackedDeceasedListItem item, CancellationToken cancellationToken)
    {
        if (item.NotifyOnBirthAnniversary && item.BirthDate is DateOnly birth)
        {
            await notificationScheduler.ScheduleAnniversaryAsync(
                new AnniversaryReminder(item.DeceasedId, item.FullName, birth, AnniversaryKind.Birth),
                cancellationToken);
        }
        if (item.NotifyOnDeathAnniversary)
        {
            await notificationScheduler.ScheduleAnniversaryAsync(
                new AnniversaryReminder(item.DeceasedId, item.FullName, item.DeathDate, AnniversaryKind.Death),
                cancellationToken);
        }
    }
}
