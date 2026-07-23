namespace GdeOni.Application.Common.Security;

/// <summary>
/// D43. Настройки восстановления пароля по ссылке из письма.
/// Биндится из секции <c>PasswordReset</c> в appsettings.
/// </summary>
public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    /// <summary>
    /// Сколько живёт ссылка из письма. 60 минут — компромисс: меньше
    /// (10–15 мин) раздражает тех, кто читает почту не сразу; больше
    /// (сутки) держит рабочий ключ к аккаунту в почтовом ящике слишком
    /// долго.
    /// </summary>
    public int TokenLifetimeMinutes { get; set; } = 60;

    /// <summary>
    /// Базовый адрес страницы ввода нового пароля на сайте. К нему
    /// добавляется <c>?token=...</c>. Пример:
    /// <c>https://gdeoni.ru/reset-password</c>.
    ///
    /// Пустое значение = письма о сбросе не отправляются (see
    /// <see cref="IsConfigured"/>): без рабочей ссылки письмо
    /// бессмысленно, лучше честно не слать ничего.
    /// </summary>
    public string WebResetUrl { get; set; } = string.Empty;

    /// <summary>
    /// Название сервиса в подписи письма.
    /// </summary>
    public string AppName { get; set; } = "Где Они";

    /// <summary>
    /// Готова ли фича к работе. Проверяется в use case вместе с
    /// <c>IEmailSender.IsEnabled</c>.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(WebResetUrl);
}
