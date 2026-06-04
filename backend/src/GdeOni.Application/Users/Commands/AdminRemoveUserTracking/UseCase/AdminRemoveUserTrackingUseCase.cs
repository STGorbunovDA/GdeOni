using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.AdminRemoveUserTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.AdminRemoveUserTracking.UseCase;

/// <summary>
/// Снятие одного конкретного отслеживания у юзера админом. Контроллер
/// проверяет Roles=SuperAdmin/Admin. SuperAdmin как цель — отрезается
/// через 403 на уровне target.Role (паттерн как в Revoke/ChangeRole).
/// </summary>
public sealed class AdminRemoveUserTrackingUseCase(
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IAdminRemoveUserTrackingUseCase
{
    public Task<UnitResult<Error>> Execute(
        AdminRemoveUserTrackingCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        AdminRemoveUserTrackingCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTrackingByDeceasedId(
            command.UserId, command.DeceasedId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        if (user.Role == Domain.Shared.UserRole.SuperAdmin)
            return Errors.User.UserForbidden();

        var result = user.RemoveTracking(command.DeceasedId);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}

/// <summary>
/// Снятие всех отслеживаний у юзера разом. Возвращает количество удалённых
/// записей для UI-фидбэка ("Снято N отслеживаний").
/// </summary>
public sealed class AdminRemoveAllUserTrackingUseCase(
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IAdminRemoveAllUserTrackingUseCase
{
    public Task<Result<AdminRemoveAllUserTrackingResponse, Error>> Execute(
        AdminRemoveAllUserTrackingCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<AdminRemoveAllUserTrackingResponse, Error>> Handle(
        AdminRemoveAllUserTrackingCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithAllTracking(
            command.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        if (user.Role == Domain.Shared.UserRole.SuperAdmin)
            return Errors.User.UserForbidden();

        var removedCount = user.RemoveAllTracking();
        if (removedCount > 0)
            await userRepository.Save(cancellationToken);

        return Result.Success<AdminRemoveAllUserTrackingResponse, Error>(
            new AdminRemoveAllUserTrackingResponse(removedCount));
    }
}
