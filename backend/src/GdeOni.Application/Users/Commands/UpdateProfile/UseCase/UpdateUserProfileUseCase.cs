using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateProfile.UseCase;

public sealed class UpdateUserProfileUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
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

        // Сравнение в нормализованной форме: "JohnDoe" → "johndoe" — это
        // тот же логин, конфликта быть не должно.
        var normalizedNewUserName = command.UserName.Trim().ToLowerInvariant();
        var userNameExists = await userRepository.ExistsByUserName(command.UserName, cancellationToken);
        if (userNameExists && user.UserNameNormalized != normalizedNewUserName)
            return Errors.User.UserNameAlreadyExists();

        // Снимаем SecurityStamp до вызова мутации — если домен сделает
        // no-op (D11.8.2), stamp не изменится и invalidate можно
        // пропустить.
        var stampBefore = user.SecurityStamp;

        var result = user.UpdateProfile(command.UserName, command.FullName);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);
        // D11.8.1: если домен реально ротировал stamp, выбиваем кеш.
        if (user.SecurityStamp != stampBefore)
            securityStampInvalidator.Invalidate(user.Id);

        return Result.Success<UpdateUserProfileResponse, Error>(
            new UpdateUserProfileResponse(user.Id));
    }
}