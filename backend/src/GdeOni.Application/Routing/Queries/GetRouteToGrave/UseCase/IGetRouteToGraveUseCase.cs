using CSharpFunctionalExtensions;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Routing.Queries.GetRouteToGrave.UseCase;

public interface IGetRouteToGraveUseCase
{
    Task<Result<GetRouteToGraveResult, Error>> Execute(
        GetRouteToGraveQuery query,
        CancellationToken cancellationToken);
}
