using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.Validation;

public sealed class RemoveMemoryCommandValidator : AbstractValidator<RemoveMemoryCommand>
{
    public RemoveMemoryCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.MemoryId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("memory_id"));
    }
}