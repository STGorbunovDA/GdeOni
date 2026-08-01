using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Sharing.Queries.GetShareBundle.Model;
using GdeOni.Domain.Aggregates.Sharing;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Sharing.Queries.GetShareBundle.Validation;

public sealed class GetShareBundleQueryValidator : AbstractValidator<GetShareBundleQuery>
{
    public GetShareBundleQueryValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithError(Errors.Share.NotFound())
            .MaximumLength(ShareBundle.MaxCodeLength)
            .WithError(Errors.Share.NotFound());
    }
}
