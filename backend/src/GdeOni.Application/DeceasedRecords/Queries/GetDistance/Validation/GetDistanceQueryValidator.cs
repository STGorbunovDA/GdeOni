using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetDistance.Validation;

public sealed class GetDistanceQueryValidator : AbstractValidator<GetDistanceQuery>
{
    public GetDistanceQueryValidator()
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
    }
}