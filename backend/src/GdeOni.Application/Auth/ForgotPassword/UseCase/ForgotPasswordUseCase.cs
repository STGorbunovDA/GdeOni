using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Email;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.ForgotPassword.Model;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Auth.ForgotPassword.UseCase;

/// <summary>
/// D43. Выдача ссылки восстановления пароля.
///
/// ГЛАВНОЕ СВОЙСТВО: наружу всегда уходит успех — независимо от того,
/// нашёлся пользователь или нет. Иначе эндпоинт превращается в
/// перечислитель аккаунтов: злоумышленник прогоняет список адресов и по
/// коду ответа узнаёт, кто зарегистрирован в сервисе. А для сервиса про
/// умерших родственников сам факт «этот человек здесь есть» — уже
/// чувствительная информация.
///
/// По той же причине нет и защиты «письмо уже отправляли недавно»:
/// разное поведение на повтор тоже утекало бы наружу. От перебора
/// защищает rate-limit на контроллере (политика auth).
/// </summary>
public sealed class ForgotPasswordUseCase(
    IUserRepository userRepository,
    ISecureTokenFactory tokenFactory,
    IEmailSender emailSender,
    IOptions<PasswordResetOptions> passwordResetOptions,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    ILogger<ForgotPasswordUseCase> logger,
    TimeProvider timeProvider)
    : IForgotPasswordUseCase
{
    private readonly PasswordResetOptions _options = passwordResetOptions.Value;

    public Task<UnitResult<Error>> Execute(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        // Канал не настроен — ссылку доставить нечем. Не притворяемся,
        // что что-то сделали: пишем warning администратору в лог, но
        // клиенту всё равно отвечаем успехом (см. свойство выше).
        if (!emailSender.IsEnabled || !_options.IsConfigured)
        {
            logger.LogWarning(
                "D43. Запрошено восстановление пароля, но канал не готов: " +
                "Email.IsEnabled={EmailEnabled}, PasswordReset:WebResetUrl задан={UrlConfigured}. " +
                "Письмо не отправлено.",
                emailSender.IsEnabled,
                _options.IsConfigured);
            return UnitResult.Success<Error>();
        }

        var user = await userRepository.GetByEmail(command.Email, cancellationToken);
        if (user is null)
            return UnitResult.Success<Error>();

        // Заблокированному аккаунту сброс пароля не поможет — доступ
        // всё равно закрыт. Молча выходим, наружу разницы не видно.
        if (user.IsBlocked)
        {
            logger.LogInformation(
                "D43. Восстановление пароля для заблокированного пользователя {UserId} пропущено.",
                user.Id);
            return UnitResult.Success<Error>();
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var token = tokenFactory.Generate();
        var tokenHash = tokenFactory.Hash(token);
        var expiresAtUtc = nowUtc.AddMinutes(_options.TokenLifetimeMinutes);

        var requestResult = user.RequestPasswordReset(tokenHash, expiresAtUtc, nowUtc);
        if (requestResult.IsFailure)
            return requestResult.Error;

        await userRepository.Save(cancellationToken);

        var resetUrl = PasswordResetEmailContent.BuildResetUrl(_options.WebResetUrl, token);
        var message = PasswordResetEmailContent.Build(
            recipientEmail: user.Email,
            recipientName: user.FullName ?? user.UserName,
            resetUrl: resetUrl,
            lifetimeMinutes: _options.TokenLifetimeMinutes,
            appName: _options.AppName);

        try
        {
            await emailSender.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Токен уже сохранён, а письмо не ушло. Наружу это не
            // выносим (иначе тот же enumeration через таймаут/ошибку),
            // но в логе должно быть видно: юзер напишет «письмо не
            // пришло», и причина найдётся сразу.
            logger.LogError(
                ex,
                "D43. Не удалось отправить письмо восстановления пароля пользователю {UserId}.",
                user.Id);
        }

        return UnitResult.Success<Error>();
    }
}
