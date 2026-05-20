using GdeOni.Mobile.Shared.Notifications;

namespace GdeOni.Mobile.Services.Notifications;

/// <summary>
/// E23. Fallback для платформ без реализации (iOS / Windows / тесты).
/// Контракт <see cref="ILocalNotificationScheduler"/> всегда резолвится,
/// hookup-точки в ViewModel'ях работают без if'ов про платформу.
/// </summary>
public sealed class NoOpLocalNotificationScheduler : ILocalNotificationScheduler
{
    public Task ScheduleAnniversaryAsync(AnniversaryReminder reminder, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task CancelAsync(Guid deceasedId, AnniversaryKind kind, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task CancelAllForDeceasedAsync(Guid deceasedId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> EnsureNotificationPermissionAsync() => Task.FromResult(true);
}
