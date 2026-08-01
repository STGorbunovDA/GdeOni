namespace GdeOni.Application.Common.Security;

/// <summary>
/// D45. Настройки подтверждения email по ссылке из письма. Биндится из
/// секции <c>EmailConfirmation</c> в appsettings. Зеркалит
/// <see cref="PasswordResetOptions"/>.
/// </summary>
public sealed class EmailConfirmationOptions
{
    public const string SectionName = "EmailConfirmation";

    /// <summary>
    /// Сколько живёт ссылка из письма. 48 часов — подтверждение читают не
    /// так срочно, как сброс пароля: человек может открыть письмо на
    /// следующий день. Слишком короткий срок раздражал бы; слишком
    /// длинный смысла не имеет — можно перевыслать письмо из баннера.
    /// </summary>
    public int TokenLifetimeHours { get; set; } = 48;

    /// <summary>
    /// Базовый адрес страницы подтверждения на сайте. К нему добавляется
    /// <c>?token=...</c>. Пример: <c>https://gdeoni.ru/confirm-email</c>.
    ///
    /// Пустое значение = письма подтверждения не отправляются (see
    /// <see cref="IsConfigured"/>): без рабочей ссылки письмо бессмысленно.
    /// В этом случае регистрация всё равно проходит (модель мягкая), но
    /// баннер снять будет нечем, пока адрес не пропишут.
    /// </summary>
    public string WebConfirmUrl { get; set; } = string.Empty;

    /// <summary>
    /// Название сервиса в подписи письма.
    /// </summary>
    public string AppName { get; set; } = "Где Они";

    /// <summary>
    /// Готова ли фича к работе. Проверяется вместе с
    /// <c>IEmailSender.IsEnabled</c>.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(WebConfirmUrl);
}
