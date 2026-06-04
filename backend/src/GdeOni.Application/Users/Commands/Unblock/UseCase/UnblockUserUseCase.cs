using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.Unblock.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Unblock.UseCase;

public sealed class UnblockUserUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUnblockUserUseCase
{
    public Task<Result<UnblockUserResponse, Error>> Execute(
        UnblockUserCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<UnblockUserResponse, Error>> Handle(
        UnblockUserCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        var user = await userRepository.GetById(command.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        // Иерархия зеркальна Block: Admin не может разблокировать Admin,
        // иначе Admin'у было бы достаточно одного коллеги, чтобы обойти
        // SuperAdmin'ское решение о блокировке. SuperAdmin тоже не блокируется
        // — но Unblock SuperAdmin'а допускаем гипотетически, всё равно нечего
        // разблокировать (Block для него запрещён).
        var isSuperAdmin = currentUserService.IsInRole(nameof(UserRole.SuperAdmin));
        if (user.Role == UserRole.Admin && !isSuperAdmin)
            return Errors.User.BlockPeerAdminForbidden();

        var unblockResult = user.Unblock();
        if (unblockResult.IsFailure)
            return unblockResult.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<UnblockUserResponse, Error>(
            new UnblockUserResponse(user.Id));
    }
}
