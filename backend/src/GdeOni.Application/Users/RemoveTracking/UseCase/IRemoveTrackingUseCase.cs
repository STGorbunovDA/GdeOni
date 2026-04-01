using CSharpFunctionalExtensions;
using GdeOni.Application.Users.RemoveTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.RemoveTracking.UseCase;

public interface IRemoveTrackingUseCase
{
    Task<Result<RemoveTrackingResponse, Error>> Execute(
        Guid userId,
        Guid deceasedId,
        CancellationToken cancellationToken);
}