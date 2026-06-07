using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Support.Queries.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.GetAll.Validation;

public sealed class GetAllSupportTicketsQueryValidator : AbstractValidator<GetAllSupportTicketsQuery>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;
    private const int MaxSearchLength = 200;

    public GetAllSupportTicketsQueryValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithError(Errors.Pagination.PageMustBeGreaterThanZero());

        RuleFor(x => x.PageSize)
            .InclusiveBetween(MinPageSize, MaxPageSize)
            .WithError(Errors.Pagination.PageSizeOutOfRange(MinPageSize, MaxPageSize));

        RuleFor(x => x.Search)
            .MaximumLength(MaxSearchLength)
            .WithError(Errors.General.ValueIsInvalid("search"))
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x)
            .Must(x => !x.CreatedFromUtc.HasValue
                || !x.CreatedToUtc.HasValue
                || x.CreatedFromUtc.Value <= x.CreatedToUtc.Value)
            .WithError(Errors.General.ValueIsInvalid("createdFromUtc"));

        RuleFor(x => x.CreatedFromUtc)
            .LessThanOrEqualTo(_ => timeProvider.GetUtcNow().UtcDateTime)
            .WithError(Errors.General.ValueIsInvalid("createdFromUtc"))
            .When(x => x.CreatedFromUtc.HasValue);

        RuleFor(x => x.CreatedToUtc)
            .LessThanOrEqualTo(_ => timeProvider.GetUtcNow().UtcDateTime)
            .WithError(Errors.General.ValueIsInvalid("createdToUtc"))
            .When(x => x.CreatedToUtc.HasValue);
    }
}
