using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Queries.GetTrackedDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetTrackedDeceased.Validation;

public sealed class GetTrackedDeceasedQueryValidator : AbstractValidator<GetTrackedDeceasedQuery>
{
    public GetTrackedDeceasedQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(Errors.Tracking.UserIdRequired());
    }
}