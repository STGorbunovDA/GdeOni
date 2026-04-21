using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.UpdateTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateTracking.UseCase;

public interface IUpdateTrackingUseCase
{
    Task<Result<UpdateTrackingResponse, Error>> Execute(
        UpdateTrackingCommand command,
        CancellationToken cancellationToken);
}