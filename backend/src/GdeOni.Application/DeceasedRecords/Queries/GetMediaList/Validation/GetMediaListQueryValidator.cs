using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaList.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaList.Validation;

public sealed class GetMediaListQueryValidator : AbstractValidator<GetMediaListQuery>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    public GetMediaListQueryValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithError(Errors.Pagination.PageMustBeGreaterThanZero());

        RuleFor(x => x.PageSize)
            .InclusiveBetween(MinPageSize, MaxPageSize)
            .WithError(Errors.Pagination.PageSizeOutOfRange(MinPageSize, MaxPageSize));

        RuleFor(x => x.Kind!)
            .IsInEnum()
            .WithError(Errors.DeceasedMedia.KindInvalid())
            .When(x => x.Kind.HasValue);
    }
}
