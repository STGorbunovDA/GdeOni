using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.ChangeEmail.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeEmail.UseCase;

public sealed class ChangeEmailUseCase(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ICurrentUserService currentUserService,
    ISecurityStampInvalidator securityStampInvalidator,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IChangeEmailUseCase
{
    public Task<Result<ChangeEmailResponse, Error>> Execute(
        ChangeEmailCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<ChangeEmailResponse, Error>> Handle(
        ChangeEmailCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();
        
        var user = await userRepository.GetById(command.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        if (!isAdmin && user.Id != currentUserId)
            return Errors.User.UserForbidden();
        
        var exists = await userRepository.ExistsByEmail(command.Email, cancellationToken);
        if (exists && !string.Equals(user.Email, command.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            return Errors.User.EmailAlreadyExists();

        var stampBefore = user.SecurityStamp;

        var result = user.ChangeEmail(command.Email);
        if (result.IsFailure)
            return result.Error;

        // No-op (D11.8.2): тот же email, домен ничего не менял —
        // не дёргаем Save, не отзываем токены, не сбрасываем кеш.
        if (user.SecurityStamp == stampBefore)
        {
            return Result.Success<ChangeEmailResponse, Error>(
                new ChangeEmailResponse(user.Id, user.Email));
        }

        // Сначала фиксируем смену email + новый SecurityStamp,
        // затем форс-логаут всех сессий. Порядок и trade-off — как в
        // ChangePasswordUseCase после D7.36 (RevokeAllForUser коммитит сразу).
        // Изменение email = security-event: bad guy с украденным RT не должен
        // переживать восстановление аккаунта (D7.41).
        await userRepository.Save(cancellationToken);
        await refreshTokenRepository.RevokeAllForUser(user.Id, cancellationToken);
        // Закрываем окно валидности access-токенов в кеше OnTokenValidated
        // (D11.8.1) — иначе старый токен живёт ≤ SecurityStampCacheTtlSeconds.
        securityStampInvalidator.Invalidate(user.Id);

        return Result.Success<ChangeEmailResponse, Error>(
            new ChangeEmailResponse(user.Id, user.Email));
    }
}