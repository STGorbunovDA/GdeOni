using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.Validation;

public sealed class GetAgeAtDeathQueryValidator : AbstractValidator<GetAgeAtDeathQuery>
{
    public GetAgeAtDeathQueryValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());
    }
}