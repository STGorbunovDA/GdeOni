using CSharpFunctionalExtensions;
using GdeOni.Application.Geo.Queries.ForwardGeocode.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Geo.Queries.ForwardGeocode.UseCase;

public interface IForwardGeocodeUseCase
{
    Task<Result<ForwardGeocodeResponse, Error>> Execute(
        ForwardGeocodeQuery query,
        CancellationToken cancellationToken);
}
