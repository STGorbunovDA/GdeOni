using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Validation;

public sealed class RejectMemoryCommandValidator : AbstractValidator<RejectMemoryCommand>
{
    public RejectMemoryCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.MemoryId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("memory_id"));
    }
}