using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.ChangeRole.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeRole.UseCase;

public sealed class ChangeRoleUseCase(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IChangeRoleUseCase
{
    public Task<Result<ChangeRoleResponse, Error>> Execute(
        ChangeRoleCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<ChangeRoleResponse, Error>> Handle(
        ChangeRoleCommand command,
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

        var isSuperAdmin = currentUserService.IsInRole(nameof(UserRole.SuperAdmin));

        // SuperAdmin неприкасаем для всех, кроме другого SuperAdmin.
        // Сейчас SuperAdmin создаётся только сидером (D7.14), поэтому
        // фактически SuperAdmin может только сам себе понизить роль —
        // дополнительная защита от случайного «ох, я кликнул не туда».
        if (user.Role == UserRole.SuperAdmin && !isSuperAdmin)
            return Errors.User.ChangeSuperAdminRoleForbidden();

        // Admin не может менять роли других Admin — только SuperAdmin.
        // Это защищает от admin-vs-admin войн и от понижения коллег.
        if (user.Role == UserRole.Admin && !isSuperAdmin)
            return Errors.User.ChangePeerAdminRoleForbidden();

        var result = user.ChangeRole(command.UserRole);
        if (result.IsFailure)
            return result.Error;

        // Сначала фиксируем смену роли + новый SecurityStamp, затем
        // форс-логаут всех сессий. ChangeRole — security-event:
        // при разжаловании старые refresh-токены не должны выдавать
        // новые access-токены, при повышении старые токены не должны
        // продолжать жить с прежним claim'ом роли (D7.41).
        await userRepository.Save(cancellationToken);
        await refreshTokenRepository.RevokeAllForUser(user.Id, cancellationToken);

        return Result.Success<ChangeRoleResponse, Error>(
            new ChangeRoleResponse(user.Id));
    }
}