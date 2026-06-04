using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.Block.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Block.UseCase;

public sealed class BlockUserUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    ISecurityStampInvalidator securityStampInvalidator,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IBlockUserUseCase
{
    public Task<Result<BlockUserResponse, Error>> Execute(
        BlockUserCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<BlockUserResponse, Error>> Handle(
        BlockUserCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        if (command.UserId == currentUserIdResult.Value)
            return Errors.User.BlockSelfForbidden();

        var user = await userRepository.GetById(command.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        // SuperAdmin неблокируем — параллель с DeleteUserUseCase (D7.14):
        // ровно одна "несменяемая" роль, чтобы система не могла случайно
        // запереть саму себя.
        if (user.Role == UserRole.SuperAdmin)
            return Errors.User.BlockSuperAdminForbidden();

        // Admin не может блокировать другого Admin — симметрично с
        // DeleteUserUseCase. Только SuperAdmin может блокировать Admin.
        var isSuperAdmin = currentUserService.IsInRole(nameof(UserRole.SuperAdmin));
        if (user.Role == UserRole.Admin && !isSuperAdmin)
            return Errors.User.BlockPeerAdminForbidden();

        var blockResult = user.Block(currentUserIdResult.Value, command.Reason, DateTime.UtcNow);
        if (blockResult.IsFailure)
            return blockResult.Error;

        await userRepository.Save(cancellationToken);

        // SecurityStamp поменялся внутри Block(): закрываем окно кеша,
        // чтобы заблокированный юзер не дожил до конца TTL access-токена.
        securityStampInvalidator.Invalidate(user.Id);

        return Result.Success<BlockUserResponse, Error>(
            new BlockUserResponse(user.Id, user.BlockedAtUtc!.Value));
    }
}
