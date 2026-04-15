using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.RemoveTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.RemoveTracking.UseCase;

public interface IRemoveTrackingUseCase
{
    Task<Result<RemoveTrackingResponse, Error>> Execute(
        RemoveTrackingCommand command,
        CancellationToken cancellationToken);
}