using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Geo.Queries.ReverseGeocode.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Geo.Queries.ReverseGeocode.Validation;

public sealed class ReverseGeocodeQueryValidator : AbstractValidator<ReverseGeocodeQuery>
{
    public ReverseGeocodeQueryValidator()
    {
        // Те же коды, что и при сохранении места захоронения — клиент уже
        // умеет их показывать.
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithError(Errors.BurialLocation.LatitudeInvalid());

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithError(Errors.BurialLocation.LongitudeInvalid());
    }
}
