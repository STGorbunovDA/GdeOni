using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.UseCase;

/// <summary>
/// Админский revoke подписки у конкретного юзера. Authorize-роль
/// проверяется на контроллере. Доменные ограничения:
///   - себе не снимаем (Admin не может выстрелить себе в ногу);
///   - Admin не управляет SuperAdmin'ом.
/// </summary>
public sealed class RevokeSubscriptionByAdminUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : IRevokeSubscriptionByAdminUseCase
{
    public Task<UnitResult<Error>> Execute(
        RevokeSubscriptionByAdminCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        RevokeSubscriptionByAdminCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (currentUserIdResult.Value == command.UserId)
            return Errors.Subscription.RevokeSelfForbidden();

        var target = await userRepository.GetById(command.UserId, cancellationToken);
        if (target is null)
            return Errors.General.NotFound("user", command.UserId);

        // Admin не управляет SuperAdmin'ом или другим Admin'ом — только
        // SuperAdmin может. Сам себя уже отсекли выше.
        var isCurrentSuperAdmin = currentUserService.IsInRole(
            Domain.Shared.UserRole.SuperAdmin.ToString());
        if ((target.Role == Domain.Shared.UserRole.SuperAdmin
                || target.Role == Domain.Shared.UserRole.Admin)
            && !isCurrentSuperAdmin)
        {
            return Errors.Subscription.ManageSuperAdminForbidden();
        }

        var revokeResult = target.RevokeSubscriptionByAdmin(timeProvider.GetUtcNow().UtcDateTime);
        if (revokeResult.IsFailure)
            return revokeResult.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
