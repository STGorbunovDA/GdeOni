using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.ResetPassword.Model;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.ResetPassword.UseCase;

/// <summary>
/// D43. Установка нового пароля по токену из письма.
///
/// В отличие от <c>ForgotPasswordUseCase</c>, здесь ошибку скрывать не
/// нужно и вредно: человек уже перешёл по ссылке, и ему надо понятно
/// сказать «ссылка устарела, запросите новую». Enumeration тут не
/// возникает — токен не подбирается (32 случайных байта).
/// </summary>
public sealed class ResetPasswordUseCase(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ISecureTokenFactory tokenFactory,
    IPasswordHasher passwordHasher,
    ISecurityStampInvalidator securityStampInvalidator,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : IResetPasswordUseCase
{
    public Task<UnitResult<Error>> Execute(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenFactory.Hash(command.Token);

        var user = await userRepository.GetByPasswordResetTokenHash(tokenHash, cancellationToken);
        if (user is null)
            return Errors.User.PasswordResetTokenInvalid();

        // Заблокированный аккаунт разблокировать сбросом пароля нельзя.
        if (user.IsBlocked)
            return Errors.User.PasswordResetTokenInvalid();

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var newPasswordHash = passwordHasher.Hash(command.NewPassword);

        // Срок действия и одноразовость проверяет сам агрегат.
        var result = user.ResetPasswordByToken(tokenHash, newPasswordHash, nowUtc);
        if (result.IsFailure)
            return result.Error;

        // Порядок как в ChangePasswordUseCase (см. комментарий там):
        // сначала фиксируем пароль + новый SecurityStamp, затем гасим
        // refresh-токены, затем сбрасываем кеш стампа.
        await userRepository.Save(cancellationToken);
        await refreshTokenRepository.RevokeAllForUser(user.Id, cancellationToken);
        securityStampInvalidator.Invalidate(user.Id);

        return UnitResult.Success<Error>();
    }
}
