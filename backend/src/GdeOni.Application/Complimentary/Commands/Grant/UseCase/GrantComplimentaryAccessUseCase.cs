using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Complimentary.Commands.Grant.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Complimentary.Commands.Grant.UseCase;

/// <summary>
/// D22. Выдача бесплатного доступа. Запрашивающий — Admin или SuperAdmin
/// (проверяется через AuthorizeAttribute на контроллере и дополнительно
/// здесь через ICurrentUserService.IsAdmin). Доменные ограничения:
/// - себе не выдают (бессмысленно — админам подписка не нужна);
/// - Admin не управляет SuperAdmin'ом (только SuperAdmin может).
/// </summary>
public sealed class GrantComplimentaryAccessUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor executor,
    ISecurityStampInvalidator securityStampInvalidator,
    TimeProvider timeProvider)
    : IGrantComplimentaryAccessUseCase
{
    public Task<UnitResult<Error>> Execute(
        GrantComplimentaryAccessCommand command,
        CancellationToken cancellationToken) =>
        executor.Execute(command, Handle, cancellationToken);

    private async Task<UnitResult<Error>> Handle(
        GrantComplimentaryAccessCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var adminId = currentUserIdResult.Value;

        if (command.TargetUserId == adminId)
            return Errors.Complimentary.GrantToSelfForbidden();

        var target = await userRepository.GetById(command.TargetUserId, cancellationToken);
        if (target is null)
            return Errors.General.NotFound("user", command.TargetUserId);

        // Admin (не SuperAdmin) не имеет прав управлять SuperAdmin'ом
        // или другим Admin'ом — только SuperAdmin может.
        var isSuperAdmin = currentUserService.IsInRole(nameof(UserRole.SuperAdmin));
        if ((target.Role == UserRole.SuperAdmin || target.Role == UserRole.Admin)
            && !isSuperAdmin)
        {
            return Errors.Complimentary.ManageSuperAdminForbidden();
        }

        var grantResult = target.GrantComplimentaryAccess(
            adminId,
            command.UntilUtc,
            command.Note,
            timeProvider.GetUtcNow().UtcDateTime);

        if (grantResult.IsFailure)
            return grantResult.Error;

        await userRepository.Save(cancellationToken);

        // D22 + D11.8: после grant'а статус доступа таргета меняется, но
        // кеш ActiveSubscriptionAuthorizationHandler ещё держит старый
        // hasAccess=false до конца TTL (default 30s). Инвалидируем явно,
        // чтобы юзер получил доступ немедленно. Invalidate чистит и
        // security-stamp, и subscription-access кеши.
        securityStampInvalidator.Invalidate(command.TargetUserId);

        return UnitResult.Success<Error>();
    }
}
