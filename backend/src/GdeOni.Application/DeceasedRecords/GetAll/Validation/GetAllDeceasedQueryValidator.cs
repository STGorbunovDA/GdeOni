using FluentValidation;
using GdeOni.Application.DeceasedRecords.GetAll.Model;

namespace GdeOni.Application.DeceasedRecords.GetAll.Validation;

public sealed class GetAllDeceasedQueryValidator
    : AbstractValidator<GetAllDeceasedQuery>
{
    public GetAllDeceasedQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .WithMessage("Search must be at most 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.Country)
            .MaximumLength(200)
            .WithMessage("Country must be at most 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Country));

        RuleFor(x => x.City)
            .MaximumLength(200)
            .WithMessage("City must be at most 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x)
            .Must(x => !x.CreatedFrom.HasValue || !x.CreatedTo.HasValue || x.CreatedFrom.Value <= x.CreatedTo.Value)
            .WithMessage("CreatedFrom must be less than or equal to CreatedTo.");
    }
}