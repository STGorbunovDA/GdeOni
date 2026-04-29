using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.Validation;

public sealed class GetMyTrackedDeceasedListQueryValidator
    : AbstractValidator<GetMyTrackedDeceasedListQuery>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    public GetMyTrackedDeceasedListQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithError(Errors.Pagination.PageMustBeGreaterThanZero());

        RuleFor(x => x.PageSize)
            .InclusiveBetween(MinPageSize, MaxPageSize)
            .WithError(Errors.Pagination.PageSizeOutOfRange(MinPageSize, MaxPageSize));
    }
}
