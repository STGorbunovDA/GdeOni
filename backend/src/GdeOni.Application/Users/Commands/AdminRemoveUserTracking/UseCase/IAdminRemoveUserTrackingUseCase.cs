using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.AdminRemoveUserTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.AdminRemoveUserTracking.UseCase;

public interface IAdminRemoveUserTrackingUseCase
{
    Task<UnitResult<Error>> Execute(
        AdminRemoveUserTrackingCommand command,
        CancellationToken cancellationToken);
}

public interface IAdminRemoveAllUserTrackingUseCase
{
    Task<Result<AdminRemoveAllUserTrackingResponse, Error>> Execute(
        AdminRemoveAllUserTrackingCommand command,
        CancellationToken cancellationToken);
}
