using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.ConfirmEmail.Model;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.ConfirmEmail.UseCase;

/// <summary>
/// D45. Подтверждение адреса email по токену из письма.
///
/// Ошибку показываем честно (в отличие от resend): человек уже перешёл по
/// ссылке, и ему надо понятно сказать «ссылка устарела, запросите новую».
/// Enumeration тут не возникает — токен не подбирается (32 случайных байта).
/// </summary>
public sealed class ConfirmEmailUseCase(
    IUserRepository userRepository,
    ISecureTokenFactory tokenFactory,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : IConfirmEmailUseCase
{
    public Task<UnitResult<Error>> Execute(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenFactory.Hash(command.Token);

        var user = await userRepository.GetByEmailConfirmationTokenHash(tokenHash, cancellationToken);
        if (user is null)
            return Errors.User.EmailConfirmationTokenInvalid();

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // Срок действия и одноразовость (клик дважды) проверяет сам агрегат.
        var result = user.ConfirmEmailByToken(tokenHash, nowUtc);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
