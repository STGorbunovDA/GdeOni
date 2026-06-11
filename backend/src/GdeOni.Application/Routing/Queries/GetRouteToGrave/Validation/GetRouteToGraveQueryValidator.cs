using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Routing.Queries.GetRouteToGrave.Validation;

public sealed class GetRouteToGraveQueryValidator : AbstractValidator<GetRouteToGraveQuery>
{
    public GetRouteToGraveQueryValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.FromLat)
            .InclusiveBetween(-90, 90)
            .WithError(Errors.BurialLocation.LatitudeInvalid());

        RuleFor(x => x.FromLon)
            .InclusiveBetween(-180, 180)
            .WithError(Errors.BurialLocation.LongitudeInvalid());

        RuleFor(x => x.Mode)
            .IsInEnum()
            .WithError(Errors.Routing.RoutingModeInvalid());
    }
}
