using FluentValidation;
using GdeOni.Application.DeceasedRecords.Create.Model;

namespace GdeOni.Application.DeceasedRecords.Create.Validation;

public sealed class CreateDeceasedRequestValidator : AbstractValidator<CreateDeceasedRequest>
{
    public CreateDeceasedRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must be at most 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must be at most 100 characters.");

        RuleFor(x => x.MiddleName)
            .MaximumLength(100).WithMessage("Middle name must be at most 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.MiddleName));

        RuleFor(x => x.DeathDate)
            .NotEmpty().WithMessage("Death date is required.");
        
        RuleFor(x => x.DeathDate)
            .NotEmpty().WithMessage("Death date is required.")
            .Must(x => x.Date <= DateTime.UtcNow.Date)
            .WithMessage("Death date cannot be in the future.");

        RuleFor(x => x)
            .Must(x => x.BirthDate is null || x.BirthDate.Value.Date <= x.DeathDate.Date)
            .WithMessage("Birth date must be less than or equal to death date.");

        RuleFor(x => x.ShortDescription)
            .MaximumLength(1000).WithMessage("Short description must be at most 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ShortDescription));

        RuleFor(x => x.Biography)
            .MaximumLength(10000).WithMessage("Biography must be at most 10000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Biography));

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("CreatedByUserId is required.");

        RuleFor(x => x.BurialLocation)
            .NotNull().WithMessage("Burial location is required.");

        When(x => x.BurialLocation is not null, () =>
        {
            RuleFor(x => x.BurialLocation!)
                .SetValidator(new CreateDeceasedBurialLocationDtoValidator());
        });

        RuleForEach(x => x.Photos)
            .SetValidator(new CreateDeceasedPhotoDtoValidator());

        RuleForEach(x => x.Memories)
            .SetValidator(new CreateDeceasedMemoryDtoValidator());

        When(x => x.Metadata is not null, () =>
        {
            RuleFor(x => x.Metadata!)
                .SetValidator(new CreateDeceasedMetadataDtoValidator());
        });
    }
}