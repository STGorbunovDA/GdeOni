namespace GdeOni.Infrastructure.Notifications.Push;

/// <summary>
/// VAPID-ключи для Web Push. Пара генерируется ОДИН раз и живёт в
/// appsettings: сменишь — все существующие подписки станут недействительны,
/// и людям придётся включать уведомления заново.
///
/// Публичный ключ уходит на клиент (через <c>/api/app/features</c>) —
/// он не секрет. Приватный не покидает сервер.
///
/// Если ключи не заданы, push просто отключён: приложение работает как
/// раньше (письма + «колокольчик»), в DI подставляется no-op отправитель.
/// Зеркалит подход EmailOptions/SmtpEmailSender.
/// </summary>
public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";

    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// Контакт владельца сервиса для push-сервисов: «mailto:...» или URL
    /// сайта. Требование спецификации VAPID — по нему с вами свяжутся, если
    /// рассылка начнёт вести себя как спам.
    /// </summary>
    public string Subject { get; set; } = "mailto:admin@gdeoni.ru";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PublicKey)
        && !string.IsNullOrWhiteSpace(PrivateKey)
        && !string.IsNullOrWhiteSpace(Subject);
}
