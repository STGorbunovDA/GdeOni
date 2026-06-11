using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Validation;

public sealed class GetNearbyDeceasedQueryValidator
    : AbstractValidator<GetNearbyDeceasedQuery>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    public const int MinRadiusMeters = 10;
    public const int MaxRadiusMeters = 5000;

    public GetNearbyDeceasedQueryValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithError(Errors.BurialLocation.LatitudeInvalid());

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithError(Errors.BurialLocation.LongitudeInvalid());

        RuleFor(x => x.RadiusMeters)
            .InclusiveBetween(MinRadiusMeters, MaxRadiusMeters)
            .WithError(Errors.NearbySearch.RadiusOutOfRange(MinRadiusMeters, MaxRadiusMeters));

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithError(Errors.Pagination.PageMustBeGreaterThanZero());

        RuleFor(x => x.PageSize)
            .InclusiveBetween(MinPageSize, MaxPageSize)
            .WithError(Errors.Pagination.PageSizeOutOfRange(MinPageSize, MaxPageSize));
    }
}
