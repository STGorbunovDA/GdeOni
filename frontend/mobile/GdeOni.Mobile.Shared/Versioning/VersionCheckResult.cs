namespace GdeOni.Mobile.Shared.Versioning;

/// <summary>
/// E22. Решение по результату проверки версии — что показать юзеру:
/// </summary>
public enum VersionCheckOutcome
{
    /// <summary>Версия достаточна — пускать в приложение.</summary>
    Ok = 0,

    /// <summary>
    /// Есть обновление, но клиент ещё поддерживается (current >= min,
    /// но &lt; latest). UI: soft banner на главной "Доступна новая
    /// версия — скачайте на сайте".
    /// </summary>
    SoftUpdateAvailable = 1,

    /// <summary>
    /// Клиент ниже MinSupportedVersion — закрывает приложение
    /// blocking-страницей с кнопкой "Скачать обновление".
    /// </summary>
    ForceUpdate = 2,
}

/// <summary>
/// E22. Результат проверки версии: outcome + payload для UI.
/// DownloadUrl/Message приходят с бэка как есть, мобилка их не
/// модифицирует.
/// </summary>
public sealed record VersionCheckResult(
    VersionCheckOutcome Outcome,
    string? DownloadUrl,
    string? ForceUpdateMessage)
{
    public static VersionCheckResult Ok() => new(VersionCheckOutcome.Ok, null, null);

    public static VersionCheckResult Soft(string? downloadUrl) =>
        new(VersionCheckOutcome.SoftUpdateAvailable, downloadUrl, null);

    public static VersionCheckResult Force(string? downloadUrl, string? message) =>
        new(VersionCheckOutcome.ForceUpdate, downloadUrl, message);
}