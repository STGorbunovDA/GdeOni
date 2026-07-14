using CSharpFunctionalExtensions;
using GdeOni.Application.Geo.Queries.ReverseGeocode.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Geo.Queries.ReverseGeocode.UseCase;

public interface IReverseGeocodeUseCase
{
    Task<Result<ReverseGeocodeResponse, Error>> Execute(
        ReverseGeocodeQuery query,
        CancellationToken cancellationToken);
}
