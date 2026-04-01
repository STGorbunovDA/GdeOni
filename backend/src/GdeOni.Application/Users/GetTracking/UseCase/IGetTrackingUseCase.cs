using CSharpFunctionalExtensions;
using GdeOni.Application.Users.GetTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.GetTracking.UseCase;

public interface IGetTrackingUseCase
{
    Task<Result<GetTrackingResponse, Error>> Execute(
        Guid userId,
        Guid deceasedId,
        CancellationToken cancellationToken);
}