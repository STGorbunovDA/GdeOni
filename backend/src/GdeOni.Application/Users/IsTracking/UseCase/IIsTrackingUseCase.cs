using CSharpFunctionalExtensions;
using GdeOni.Application.Users.IsTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.IsTracking.UseCase;

public interface IIsTrackingUseCase
{
    Task<Result<IsTrackingResponse, Error>> Execute(
        Guid userId,
        Guid deceasedId,
        CancellationToken cancellationToken);
}