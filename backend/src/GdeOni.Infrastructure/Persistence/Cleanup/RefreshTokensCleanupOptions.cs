namespace GdeOni.Infrastructure.Persistence.Cleanup;

public sealed class RefreshTokensCleanupOptions
{
    public const string SectionName = "RefreshTokensCleanup";

    /// <summary>
    /// Включает фоновый RefreshTokensCleanupService.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Интервал между прогонами cleanup, в часах.
    /// </summary>
    public int IntervalHours { get; set; } = 24;

    /// <summary>
    /// Сколько дней хранить revoked-токены до удаления. Запас нужен,
    /// чтобы D7.32 (replay detection) и аудит могли поднять историю
    /// ротации; после этого срока строка уже бесполезна.
    /// </summary>
    public int RevokedRetentionDays { get; set; } = 30;

    /// <summary>
    /// Сколько дней хранить expired-токены после ExpiresAtUtc до удаления.
    /// Короткий запас на случай дебага и расследования инцидентов
    /// («юзер вошёл вчера в это время»).
    /// </summary>
    public int ExpiredRetentionDays { get; set; } = 7;

    /// <summary>
    /// Задержка перед первым прогоном после старта приложения.
    /// </summary>
    public int InitialDelayMinutes { get; set; } = 5;
}
