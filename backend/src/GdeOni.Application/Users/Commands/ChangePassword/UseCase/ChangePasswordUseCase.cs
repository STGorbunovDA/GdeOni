using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.ChangePassword.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangePassword.UseCase;

public sealed class ChangePasswordUseCase(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IChangePasswordUseCase
{
    public Task<Result<ChangePasswordResponse, Error>> Execute(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<ChangePasswordResponse, Error>> Handle(
        ChangePasswordCommand command,
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

        // Admin сбрасывает пароль другому пользователю — не знает чужой текущий
        // пароль, поэтому проверка CurrentPassword пропускается. При смене
        // собственного пароля admin тоже обязан подтвердить текущий.
        var requiresCurrentPasswordCheck = !isAdmin || user.Id == currentUserId;
        if (requiresCurrentPasswordCheck)
        {
            if (string.IsNullOrWhiteSpace(command.CurrentPassword))
                return Errors.User.CurrentPasswordInvalid();

            var isCurrentPasswordValid =
                passwordHasher.Verify(command.CurrentPassword, user.PasswordHash);

            if (!isCurrentPasswordValid)
                return Errors.User.CurrentPasswordInvalid();
        }

        var newPasswordHash = passwordHasher.Hash(command.NewPassword);

        var result = user.ChangePasswordHash(newPasswordHash);
        if (result.IsFailure)
            return result.Error;

        // После смены пароля все активные сессии должны быть инвалидированы.
        // Новый пароль = новый старт; старые refresh-токены могли быть
        // украдены, по ним нельзя продолжать пересоздавать access-токены.
        // Save() ниже зафиксирует и user, и токены одной транзакцией —
        // оба репозитория делят AppDbContext через scope.
        await refreshTokenRepository.RevokeAllForUser(user.Id, cancellationToken);

        await userRepository.Save(cancellationToken);

        return Result.Success<ChangePasswordResponse, Error>(
            new ChangePasswordResponse(user.Id));
    }
}