using FluentValidation;
using GdeOni.Application.DeceasedRecords.UpdateMemory.Model;

namespace GdeOni.Application.DeceasedRecords.UpdateMemory.Validation;

public sealed class UpdateMemoryRequestValidator : AbstractValidator<UpdateMemoryRequest>
{
    public UpdateMemoryRequestValidator()
    {
        RuleFor(x => x.DeceasedId).NotEmpty();
        RuleFor(x => x.MemoryId).NotEmpty();

        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(5000);

        RuleFor(x => x.AuthorDisplayName)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.AuthorDisplayName));
    }
}