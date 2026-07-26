using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateProfile.UseCase;

public sealed class UpdateUserProfileUseCase(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ICurrentUserService currentUserService,
    IPasswordHasher passwordHasher,
    ISecurityStampInvalidator securityStampInvalidator,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateUserProfileUseCase
{
    public Task<Result<UpdateUserProfileResponse, Error>> Execute(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<UpdateUserProfileResponse, Error>> Handle(
        UpdateUserProfileCommand command,
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

        // Current-password check: защита от vandalism через украденный
        // access-токен. Если клиент прислал CurrentPassword — обязан
        // совпасть. Если не прислал — пропускаем (backward-compat
        // со старыми клиентами). Админ правит чужой профиль без
        // проверки пароля цели — у него уже есть права.
        if (command.CurrentPassword is not null && user.Id == currentUserId)
        {
            if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
                return Errors.User.CurrentPasswordInvalid();
        }

        // Имя пользователя (UserName) не уникально — тёзки допустимы (это
        // отображаемое имя, вход по email). Проверка уникальности убрана
        // вместе с уникальным индексом (миграция RemoveUserNameUniqueIndex).

        // Снимаем SecurityStamp до вызова мутации — если домен сделает
        // no-op (D11.8.2), stamp не изменится и invalidate можно
        // пропустить.
        var stampBefore = user.SecurityStamp;

        var result = user.UpdateProfile(command.UserName, command.FullName);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);
        if (user.SecurityStamp != stampBefore)
        {
            // D11.8.1: если домен реально ротировал stamp, выбиваем кеш.
            securityStampInvalidator.Invalidate(user.Id);
            // Также ревокаем refresh-токены: атакующий с украденным access
            // мог перевыпустить access через /refresh ещё ~14 дней,
            // несмотря на изменение профиля. Симметрично ChangePassword
            // (D7.36). Сам юзер просто переавторизуется на устройстве.
            await refreshTokenRepository.RevokeAllForUser(user.Id, cancellationToken);
        }

        return Result.Success<UpdateUserProfileResponse, Error>(
            new UpdateUserProfileResponse(user.Id));
    }
}