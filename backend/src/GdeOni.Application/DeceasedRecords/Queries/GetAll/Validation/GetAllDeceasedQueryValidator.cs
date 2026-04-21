using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetAll.Validation;

public sealed class GetAllDeceasedQueryValidator : AbstractValidator<GetAllDeceasedQuery>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;
    private const int MaxSearchLength = 200;
    private const int MaxCountryLength = 200;
    private const int MaxCityLength = 200;

    public GetAllDeceasedQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithError(Errors.Pagination.PageMustBeGreaterThanZero());

        RuleFor(x => x.PageSize)
            .InclusiveBetween(MinPageSize, MaxPageSize)
            .WithError(Errors.Pagination.PageSizeOutOfRange(MinPageSize, MaxPageSize));

        RuleFor(x => x.Search)
            .MaximumLength(MaxSearchLength)
            .WithError(Errors.Deceased.SearchTooLong(MaxSearchLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.Country)
            .MaximumLength(MaxCountryLength)
            .WithError(Errors.BurialLocation.CountryTooLong(MaxCountryLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Country));

        RuleFor(x => x.City)
            .MaximumLength(MaxCityLength)
            .WithError(Errors.BurialLocation.CityTooLong(MaxCityLength))
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x)
            .Must(x => !x.CreatedFrom.HasValue || !x.CreatedTo.HasValue || x.CreatedFrom.Value <= x.CreatedTo.Value)
            .WithError(Errors.Deceased.CreatedFromMustBeLessOrEqualToCreatedTo());

        RuleFor(x => x.CreatedFrom)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithError(Errors.Deceased.CreatedFromInFuture())
            .When(x => x.CreatedFrom.HasValue);

        RuleFor(x => x.CreatedTo)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithError(Errors.Deceased.CreatedToInFuture())
            .When(x => x.CreatedTo.HasValue);
    }
}