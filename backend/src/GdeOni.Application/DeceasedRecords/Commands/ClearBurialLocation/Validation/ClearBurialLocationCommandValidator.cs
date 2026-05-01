using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.ClearBurialLocation.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ClearBurialLocation.Validation;

public sealed class ClearBurialLocationCommandValidator : AbstractValidator<ClearBurialLocationCommand>
{
    public ClearBurialLocationCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());
    }
}
