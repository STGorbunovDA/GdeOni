using FluentValidation;
using GdeOni.Application.Users.Queries.GetTracking.Model;

namespace GdeOni.Application.Users.Queries.GetTracking.Validation;

public sealed class GetTrackingQueryValidator : AbstractValidator<GetTrackingQuery>
{
    public GetTrackingQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.DeceasedId)
            .NotEmpty();
    }
}