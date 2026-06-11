using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Complimentary.Commands.Revoke.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Complimentary.Commands.Revoke.UseCase;

/// <summary>
/// D22. Отзыв бесплатного доступа. Те же права-проверки что и у
/// <see cref="Grant.UseCase.GrantComplimentaryAccessUseCase"/>: только
/// Admin/SuperAdmin может, Admin не управляет SuperAdmin'ом.
/// </summary>
public sealed class RevokeComplimentaryAccessUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor executor,
    ISecurityStampInvalidator securityStampInvalidator)
    : IRevokeComplimentaryAccessUseCase
{
    public Task<UnitResult<Error>> Execute(
        RevokeComplimentaryAccessCommand command,
        CancellationToken cancellationToken) =>
        executor.Execute(command, Handle, cancellationToken);

    private async Task<UnitResult<Error>> Handle(
        RevokeComplimentaryAccessCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        var target = await userRepository.GetById(command.TargetUserId, cancellationToken);
        if (target is null)
            return Errors.General.NotFound("user", command.TargetUserId);

        var isSuperAdmin = currentUserService.IsInRole(nameof(UserRole.SuperAdmin));
        if ((target.Role == UserRole.SuperAdmin || target.Role == UserRole.Admin)
            && !isSuperAdmin)
        {
            return Errors.Complimentary.ManageSuperAdminForbidden();
        }

        var revokeResult = target.RevokeComplimentaryAccess();
        if (revokeResult.IsFailure)
            return revokeResult.Error;

        await userRepository.Save(cancellationToken);

        // Симметрично Grant: после revoke юзер должен потерять доступ
        // мгновенно, без ожидания TTL кеша.
        securityStampInvalidator.Invalidate(command.TargetUserId);

        return UnitResult.Success<Error>();
    }
}
