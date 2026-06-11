using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Validation;

public sealed class GetMyTrackedDeceasedDetailsQueryValidator
    : AbstractValidator<GetMyTrackedDeceasedDetailsQuery>
{
    public GetMyTrackedDeceasedDetailsQueryValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());
    }
}
