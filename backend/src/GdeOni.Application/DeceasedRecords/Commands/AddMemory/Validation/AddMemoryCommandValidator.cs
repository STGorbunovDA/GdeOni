using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddMemory.Validation;

public sealed class AddMemoryCommandValidator : AbstractValidator<AddMemoryCommand>
{
    public AddMemoryCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

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