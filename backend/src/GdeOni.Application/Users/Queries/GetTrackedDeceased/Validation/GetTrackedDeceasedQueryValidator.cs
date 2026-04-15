using FluentValidation;
using GdeOni.Application.Users.Queries.GetTrackedDeceased.Model;

namespace GdeOni.Application.Users.Queries.GetTrackedDeceased.Validation;

public sealed class GetTrackedDeceasedQueryValidator : AbstractValidator<GetTrackedDeceasedQuery>
{
    public GetTrackedDeceasedQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}