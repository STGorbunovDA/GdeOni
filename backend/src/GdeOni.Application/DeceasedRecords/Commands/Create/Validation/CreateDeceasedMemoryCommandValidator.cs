using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Create.Validation;

public sealed class CreateDeceasedMemoryCommandValidator
    : AbstractValidator<CreateDeceasedMemoryCommand>
{
    public CreateDeceasedMemoryCommandValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .WithError(Errors.DeceasedMemory.TextRequired())
            .MaximumLength(DeceasedMemoryEntry.MaxTextLength)
            .WithError(Errors.DeceasedMemory.TextTooLong(DeceasedMemoryEntry.MaxTextLength));
    }
}