using CSharpFunctionalExtensions;
using GdeOni.Application.Users.UpdateTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.UpdateTracking.UseCase;

public interface IUpdateTrackingUseCase
{
    Task<Result<UpdateTrackingResponse, Error>> Execute(
        UpdateTrackingRequest request,
        CancellationToken cancellationToken);
}