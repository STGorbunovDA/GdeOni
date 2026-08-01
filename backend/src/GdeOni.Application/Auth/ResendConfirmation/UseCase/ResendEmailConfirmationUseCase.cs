using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.ConfirmEmail;
using GdeOni.Application.Auth.ResendConfirmation.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GdeOni.Application.Auth.ResendConfirmation.UseCase;

/// <summary>
/// D45. Повторная отправка письма с подтверждением email.
///
/// ГЛАВНОЕ СВОЙСТВО (как у ForgotPassword): наружу всегда уходит успех —
/// нашёлся пользователь или нет, подтверждён он уже или нет. Иначе
/// эндпоинт превращается в перечислитель аккаунтов. От перебора защищает
/// rate-limit на контроллере (политика auth).
/// </summary>
public sealed class ResendEmailConfirmationUseCase(
    IUserRepository userRepository,
    IEmailConfirmationService emailConfirmationService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    ILogger<ResendEmailConfirmationUseCase> logger,
    TimeProvider timeProvider)
    : IResendEmailConfirmationUseCase
{
    public Task<UnitResult<Error>> Execute(
        ResendEmailConfirmationCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        ResendEmailConfirmationCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmail(command.Email, cancellationToken);

        // Нет юзера / уже подтверждён / канал не готов — IssueConfirmation
        // вернёт null, и мы просто ничего не шлём. Наружу — всё тот же успех.
        if (user is null)
            return UnitResult.Success<Error>();

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var message = emailConfirmationService.IssueConfirmation(user, nowUtc);
        if (message is null)
            return UnitResult.Success<Error>();

        // Токен выписан на user — сохраняем ПЕРЕД отправкой (тот же порядок,
        // что в ForgotPassword: письмо не должно вести на несохранённый токен).
        await userRepository.Save(cancellationToken);
        await emailConfirmationService.SendAsync(message, cancellationToken);

        logger.LogInformation("D45. Повторно отправлено письмо подтверждения email для {UserId}.", user.Id);
        return UnitResult.Success<Error>();
    }
}
