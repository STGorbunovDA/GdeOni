using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Delete.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Delete.Validation;

public sealed class DeleteDeceasedCommandValidator : AbstractValidator<DeleteDeceasedCommand>
{
    public DeleteDeceasedCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());
    }
}