using GdeOni.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace GdeOni.Infrastructure.Email;

/// <summary>
/// D37. Заглушка канала email на случай, когда SMTP не сконфигурирован
/// (dev / integration-тесты). Ничего не отправляет; логирует факт на
/// уровне Debug, чтобы в dev было видно «что письмо ушло бы».
/// <see cref="IsEnabled"/> = false — фоновый сервис годовщин по этому
/// флагу понимает, что реальной доставки нет, и не помечает годовщину
/// как разосланную.
/// </summary>
internal sealed class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    public bool IsEnabled => false;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Email-канал не сконфигурирован — письмо '{Subject}' для {To} не отправлено (no-op).",
            message.Subject,
            message.ToEmail);
        return Task.CompletedTask;
    }
}
