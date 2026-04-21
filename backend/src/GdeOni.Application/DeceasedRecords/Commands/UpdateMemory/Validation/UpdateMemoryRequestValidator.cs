using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Validation;

public sealed class UpdateMemoryCommandValidator : AbstractValidator<UpdateMemoryCommand>
{
    public UpdateMemoryCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.MemoryId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("memory_id"));

        RuleFor(x => x.Text)
            .NotEmpty()
            .WithError(Errors.DeceasedMemory.TextRequired())
            .MaximumLength(5000)
            .WithError(Errors.DeceasedMemory.TextTooLong(5000));
    }
}