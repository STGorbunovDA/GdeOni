using GdeOni.Application.Abstractions.Email;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Aggregates.User;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Auth.ConfirmEmail;

/// <summary>
/// D45. Реализация политики подтверждения email. Зависит только от
/// Application-абстракций (<see cref="IEmailSender"/>,
/// <see cref="ISecureTokenFactory"/>) + опций, поэтому живёт в Application,
/// а не в Infrastructure.
/// </summary>
public sealed class EmailConfirmationService(
    IEmailSender emailSender,
    ISecureTokenFactory tokenFactory,
    IOptions<EmailConfirmationOptions> options,
    ILogger<EmailConfirmationService> logger)
    : IEmailConfirmationService
{
    private readonly EmailConfirmationOptions _options = options.Value;

    public bool ChannelReady => emailSender.IsEnabled && _options.IsConfigured;

    public bool IsLoginBlocked(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.EmailConfirmationRequired && !user.IsEmailConfirmed && ChannelReady;
    }

    public EmailMessage? IssueConfirmation(User user, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.IsEmailConfirmed)
            return null;

        if (!ChannelReady)
        {
            // Канал не готов — ссылку доставить нечем. Не выписываем токен
            // (иначе он «сгорит» так и не уйдя) и пишем администратору в лог.
            logger.LogWarning(
                "D45. Подтверждение email для {UserId} не отправлено: канал не готов " +
                "(Email.IsEnabled={EmailEnabled}, WebConfirmUrl задан={UrlConfigured}).",
                user.Id,
                emailSender.IsEnabled,
                _options.IsConfigured);
            return null;
        }

        var token = tokenFactory.Generate();
        var tokenHash = tokenFactory.Hash(token);
        var expiresAtUtc = nowUtc.AddHours(_options.TokenLifetimeHours);

        var requestResult = user.RequestEmailConfirmation(tokenHash, expiresAtUtc, nowUtc);
        if (requestResult.IsFailure)
        {
            // Не должно случиться (адрес не подтверждён, срок в будущем),
            // но на всякий случай не роняем регистрацию из-за письма.
            logger.LogError(
                "D45. Не удалось выписать токен подтверждения для {UserId}: {Code}.",
                user.Id,
                requestResult.Error.Code);
            return null;
        }

        var confirmUrl = EmailConfirmationEmailContent.BuildConfirmUrl(_options.WebConfirmUrl, token);
        return EmailConfirmationEmailContent.Build(
            recipientEmail: user.Email,
            recipientName: user.DisplayName,
            confirmUrl: confirmUrl,
            lifetimeHours: _options.TokenLifetimeHours,
            appName: _options.AppName);
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        try
        {
            await emailSender.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Токен уже сохранён, а письмо не ушло. Наружу не выносим —
            // человек нажмёт «отправить повторно»; в логе причина видна.
            logger.LogError(
                ex,
                "D45. Не удалось отправить письмо подтверждения email на {ToEmail}.",
                message.ToEmail);
        }
    }
}
