namespace GdeOni.Mobile.Shared.Versioning;

/// <summary>
/// E22. Чистая логика "пускать ли клиента в приложение" — без зависимостей
/// от Refit/MAUI, легко тестируется. UI-слой передаёт сюда раcпарсенный
/// ответ <c>/api/app/version</c> и текущую версию из <c>AppInfo</c>.
///
/// Поведение при некорректных версиях с сервера: если бэк прислал
/// что-то невалидное (например, <c>"latest"</c> вместо <c>"1.2.3"</c>) —
/// возвращаем Ok, чтобы кривая конфигурация не блокировала всех
/// пользователей разом. Это сознательный fail-open: лучше пропустить
/// людей в приложение, чем устроить массовое DoS из-за опечатки в
/// appsettings.
/// </summary>
public static class AppVersionGate
{
    /// <summary>
    /// Решает что показать юзеру на основе клиентской версии и серверных
    /// порогов.
    /// </summary>
    /// <param name="currentVersion">
    /// Версия установленного APK (из <c>AppInfo.Current.VersionString</c>).
    /// </param>
    /// <param name="minSupportedVersion">
    /// <c>AppVersionResponse.MinSupportedVersion</c>. Ниже неё —
    /// ForceUpdate.
    /// </param>
    /// <param name="latestVersion">
    /// <c>AppVersionResponse.LatestVersion</c>. Между min и latest —
    /// SoftUpdate.
    /// </param>
    /// <param name="downloadUrl">
    /// <c>AppVersionResponse.DownloadUrl</c>. Пробрасывается в UI как есть.
    /// </param>
    /// <param name="forceUpdateMessage">
    /// <c>AppVersionResponse.ForceUpdateMessage</c>. Опциональный текст
    /// в blocking-окне (например, "Обновите приложение для безопасной
    /// работы").
    /// </param>
    public static VersionCheckResult Evaluate(
        string? currentVersion,
        string? minSupportedVersion,
        string? latestVersion,
        string? downloadUrl,
        string? forceUpdateMessage)
    {
        // Если клиентская версия не парсится — это баг csproj или AppInfo,
        // лучше пропустить юзера в приложение чем заблокировать.
        if (!SemVer.TryParse(currentVersion, out var current))
            return VersionCheckResult.Ok();

        if (SemVer.TryParse(minSupportedVersion, out var min) && current < min)
            return VersionCheckResult.Force(downloadUrl, forceUpdateMessage);

        if (SemVer.TryParse(latestVersion, out var latest) && current < latest)
            return VersionCheckResult.Soft(downloadUrl);

        return VersionCheckResult.Ok();
    }
}