using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Queries.IsTrackedByMe.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.IsTrackedByMe.Validation;

public sealed class IsTrackedByMeQueryValidator : AbstractValidator<IsTrackedByMeQuery>
{
    public IsTrackedByMeQueryValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());
    }
}
