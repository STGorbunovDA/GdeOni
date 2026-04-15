using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Queries.GetTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetTracking.UseCase;

public interface IGetTrackingUseCase
{
    Task<Result<GetTrackingResponse, Error>> Execute(
        GetTrackingQuery query,
        CancellationToken cancellationToken);
}