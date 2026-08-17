using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.SetEmailConfirmedByAdmin.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.SetEmailConfirmedByAdmin.UseCase;

/// <summary>
/// Ручное подтверждение/снятие подтверждения email администратором.
///
/// Зачем: человек не всегда добирается до письма — опечатка в адресе,
/// спам-фильтр, недоступный ящик. Раньше такой пользователь оставался под
/// гейтом навсегда, теперь админ может подтвердить адрес вручную.
///
/// Ограничения те же, что у прочих админских операций над учётками:
/// себе не меняем (бессмысленно), а обычный Admin не трогает SuperAdmin'а и
/// других Admin'ов — только SuperAdmin.
/// </summary>
public sealed class SetEmailConfirmedByAdminUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    ISecurityStampInvalidator securityStampInvalidator,
    TimeProvider timeProvider)
    : ISetEmailConfirmedByAdminUseCase
{
    public async Task<UnitResult<Error>> Execute(
        SetEmailConfirmedByAdminCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        var adminIdResult = currentUserService.GetCurrentUserId();
        if (adminIdResult.IsFailure)
            return adminIdResult.Error;

        if (command.TargetUserId == adminIdResult.Value)
            return Errors.User.UserForbidden();

        var target = await userRepository.GetById(command.TargetUserId, cancellationToken);
        if (target is null)
            return Errors.General.NotFound("user", command.TargetUserId);

        var isSuperAdmin = currentUserService.IsInRole(nameof(UserRole.SuperAdmin));
        if ((target.Role == UserRole.SuperAdmin || target.Role == UserRole.Admin)
            && !isSuperAdmin)
        {
            return Errors.User.UserForbidden();
        }

        var result = command.Confirmed
            ? target.ConfirmEmailByAdmin(timeProvider.GetUtcNow().UtcDateTime)
            : target.RevokeEmailConfirmationByAdmin();

        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        // Снятие подтверждения ротирует SecurityStamp внутри домена —
        // чистим кеш, чтобы активные токены отвалились сразу, а не через TTL.
        if (!command.Confirmed)
            securityStampInvalidator.Invalidate(command.TargetUserId);

        return UnitResult.Success<Error>();
    }
}
