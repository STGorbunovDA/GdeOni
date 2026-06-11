namespace GdeOni.API.Options;

/// <summary>
/// D17. Информация о версиях клиента, отдаётся анонимно через
/// <c>GET /api/app/version</c>. Биндится из секции
/// <c>AppVersion</c> в appsettings.
///
/// AllowAnonymous — потому что старый клиент с протухшим токеном
/// тоже должен узнать, что пора обновляться.
/// </summary>
public sealed class AppVersionOptions
{
    public const string SectionName = "AppVersion";

    /// <summary>
    /// Минимально поддерживаемая версия. Клиент &lt; этой версии должен
    /// показать blocking-update screen.
    /// </summary>
    public string MinSupportedVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Последняя релизная версия. Клиент с currentVersion &lt; latest
    /// показывает soft-banner "доступна новая версия".
    /// </summary>
    public string LatestVersion { get; set; } = "1.0.0";

    /// <summary>
    /// URL для скачивания APK. По решению 2026-05-19 распространяем
    /// Android-приложение через сайт (sideload), не через Play Store —
    /// поэтому здесь ссылка на gdeoni.ru/download. Web использует
    /// этот же URL для текста "обновитесь по ссылке" (на web обычно
    /// reload страницы достаточно).
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Опциональный текст для blocking-update screen. Если null —
    /// клиент использует свой дефолт ("Установите новую версию,
    /// чтобы продолжить пользоваться приложением").
    /// </summary>
    public string? ForceUpdateMessage { get; set; }
}
