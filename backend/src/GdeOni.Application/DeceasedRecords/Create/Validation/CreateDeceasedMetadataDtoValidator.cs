using FluentValidation;
using GdeOni.Application.DeceasedRecords.Create.Model;

namespace GdeOni.Application.DeceasedRecords.Create.Validation;

public sealed class CreateDeceasedMetadataDtoValidator
    : AbstractValidator<CreateDeceasedMetadataDto>
{
    public CreateDeceasedMetadataDtoValidator()
    {
        RuleFor(x => x.Epitaph)
            .MaximumLength(500).WithMessage("Epitaph must be at most 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Epitaph));

        RuleFor(x => x.Religion)
            .MaximumLength(200).WithMessage("Religion must be at most 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Religion));

        RuleFor(x => x.Source)
            .MaximumLength(500).WithMessage("Source must be at most 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Source));

        RuleFor(x => x.AdditionalInfo)
            .MaximumLength(2000).WithMessage("Additional info must be at most 2000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.AdditionalInfo));
    }
}