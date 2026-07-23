using System.Net;
using System.Net.Mail;
using GdeOni.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Email;

/// <summary>
/// D37. Отправка email через SMTP на базе <see cref="SmtpClient"/>
/// (встроен в BCL — без внешних NuGet). Покрывает STARTTLS/SSL и basic
/// auth, чего достаточно для Yandex / Mail.ru / Gmail / большинства
/// транзакционных SMTP-провайдеров.
///
/// При сбое доставки бросает исключение — вызывающий (фоновый сервис
/// годовщин) ловит его, не пишет в лог отправленных и повторит на
/// следующем прогоне.
/// </summary>
internal sealed class SmtpEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public bool IsEnabled => _options.IsConfigured;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            logger.LogDebug(
                "SMTP не сконфигурирован — письмо '{Subject}' для {To} не отправлено.",
                message.Subject,
                message.ToEmail);
            return;
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromEmail!, _options.FromName),
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8,
        };

        mail.To.Add(string.IsNullOrWhiteSpace(message.ToName)
            ? new MailAddress(message.ToEmail)
            : new MailAddress(message.ToEmail, message.ToName));

        // HTML-версия как alternate view: клиенты без HTML показывают
        // Body (plain-text), остальные — красивую вёрстку.
        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            var htmlView = AlternateView.CreateAlternateViewFromString(
                message.HtmlBody,
                System.Text.Encoding.UTF8,
                "text/html");
            mail.AlternateViews.Add(htmlView);
        }

        using var client = new SmtpClient(_options.Host!, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Timeout = _options.TimeoutSeconds * 1000,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }

        await client.SendMailAsync(mail, cancellationToken);

        logger.LogInformation(
            "Email '{Subject}' отправлен на {To}.",
            message.Subject,
            message.ToEmail);
    }
}
