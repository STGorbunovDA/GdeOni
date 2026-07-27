using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Geo;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Geo.Queries.ForwardGeocode.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Geo.Queries.ForwardGeocode.UseCase;

/// <summary>
/// Текст адреса (город / кладбище) → координаты. Форма «добавить умершего»
/// подставляет по введённому городу точку на карте, пока у пользователя ещё
/// нет координат (не нажал GPS и не тыкнул в карту). Дальше он уточняет.
/// </summary>
public sealed class ForwardGeocodeUseCase(
    IForwardGeocoder geocoder,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IForwardGeocodeUseCase
{
    public Task<Result<ForwardGeocodeResponse, Error>> Execute(
        ForwardGeocodeQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<ForwardGeocodeResponse, Error>> Handle(
        ForwardGeocodeQuery query,
        CancellationToken cancellationToken)
    {
        var result = await geocoder.Search(query.Query, cancellationToken);

        if (result.IsFailure)
            return result.Error;

        var place = result.Value;
        return Result.Success<ForwardGeocodeResponse, Error>(
            new ForwardGeocodeResponse(place.Latitude, place.Longitude, place.DisplayName));
    }
}
