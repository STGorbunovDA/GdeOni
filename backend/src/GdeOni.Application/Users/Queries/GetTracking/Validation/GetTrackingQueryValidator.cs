using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Queries.GetTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetTracking.Validation;

public sealed class GetTrackingQueryValidator : AbstractValidator<GetTrackingQuery>
{
    public GetTrackingQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(Errors.Tracking.UserIdRequired());

        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Tracking.DeceasedIdRequired());
    }
}