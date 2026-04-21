using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Create.Validation;

public sealed class CreateDeceasedMemoryCommandValidator
    : AbstractValidator<CreateDeceasedMemoryCommand>
{
    public CreateDeceasedMemoryCommandValidator()
    {
        RuleFor(x => x.AuthorUserId)
            .NotEqual(Guid.Empty)
            .WithError(Errors.User.IdRequired())
            .When(x => x.AuthorUserId.HasValue);
        
        RuleFor(x => x.Text)
            .NotEmpty()
            .WithError(Errors.DeceasedMemory.TextRequired())
            .MaximumLength(5000)
            .WithError(Errors.DeceasedMemory.TextTooLong(5000));
    }
}