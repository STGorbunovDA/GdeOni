using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.UseCase;

public interface IGetMyTrackedDeceasedDetailsUseCase
{
    Task<Result<MyTrackedDeceasedDetailsResponse, Error>> Execute(
        GetMyTrackedDeceasedDetailsQuery query,
        CancellationToken cancellationToken);
}
