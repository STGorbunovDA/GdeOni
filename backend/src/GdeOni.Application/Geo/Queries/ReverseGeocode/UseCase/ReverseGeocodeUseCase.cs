using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Geo;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Geo.Queries.ReverseGeocode.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Geo.Queries.ReverseGeocode.UseCase;

/// <summary>
/// D41. Координаты → страна / регион / город.
///
/// Используется формами «добавить умершего у могилы» и «изменить
/// координаты»: юзер получает точку с GPS или тыкает в карту, а адресные
/// поля заполняются сами. Вбивать город руками, стоя на кладбище с
/// телефоном в руке, — худшее, что можно предложить.
/// </summary>
public sealed class ReverseGeocodeUseCase(
    IReverseGeocoder geocoder,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IReverseGeocodeUseCase
{
    public Task<Result<ReverseGeocodeResponse, Error>> Execute(
        ReverseGeocodeQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<ReverseGeocodeResponse, Error>> Handle(
        ReverseGeocodeQuery query,
        CancellationToken cancellationToken)
    {
        var result = await geocoder.Reverse(
            query.Latitude,
            query.Longitude,
            cancellationToken);

        if (result.IsFailure)
            return result.Error;

        var address = result.Value;
        return Result.Success<ReverseGeocodeResponse, Error>(
            new ReverseGeocodeResponse(address.Country, address.Region, address.City));
    }
}
