using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.Validation;

public sealed class ApproveMemoryCommandValidator : AbstractValidator<ApproveMemoryCommand>
{
    public ApproveMemoryCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.MemoryId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("memory_id"));
    }
}