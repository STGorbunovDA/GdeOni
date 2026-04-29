using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.Validation;

public sealed class SetBurialLocationFromGpsCommandValidator
    : AbstractValidator<SetBurialLocationFromGpsCommand>
{
    public SetBurialLocationFromGpsCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithError(Errors.BurialLocation.LatitudeInvalid());

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithError(Errors.BurialLocation.LongitudeInvalid());

        RuleFor(x => x.AccuracyMeters)
            .GreaterThanOrEqualTo(0)
            .WithError(Errors.BurialLocation.AccuracyMetersInvalid())
            .When(x => x.AccuracyMeters.HasValue);
    }
}
