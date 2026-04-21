using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Verify.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Verify.Validation;

public sealed class VerifyDeceasedCommandValidator : AbstractValidator<VerifyDeceasedCommand>
{
    public VerifyDeceasedCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());
    }
}