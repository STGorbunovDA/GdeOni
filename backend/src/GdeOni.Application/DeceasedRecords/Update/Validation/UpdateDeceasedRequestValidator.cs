using FluentValidation;
using GdeOni.Application.DeceasedRecords.Update.Model;

namespace GdeOni.Application.DeceasedRecords.Update.Validation;

public sealed class UpdateDeceasedRequestValidator : AbstractValidator<UpdateDeceasedRequest>
{
    public UpdateDeceasedRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty().MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().MaximumLength(100);

        RuleFor(x => x.MiddleName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.MiddleName));

        RuleFor(x => x.DeathDate)
            .Must(x => x.Date <= DateTime.UtcNow.Date)
            .WithMessage("Death date cannot be in the future.");

        RuleFor(x => x)
            .Must(x => x.BirthDate is null || x.BirthDate.Value.Date <= x.DeathDate.Date)
            .WithMessage("Birth date must be less than or equal to death date.");

        RuleFor(x => x.ShortDescription)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.ShortDescription));

        RuleFor(x => x.Biography)
            .MaximumLength(10000)
            .When(x => !string.IsNullOrWhiteSpace(x.Biography));

        RuleFor(x => x.BurialLocation)
            .NotNull();

        When(x => x.BurialLocation is not null, () =>
        {
            RuleFor(x => x.BurialLocation!)
                .SetValidator(new UpdateDeceasedBurialLocationDtoValidator());
        });

        When(x => x.Metadata is not null, () =>
        {
            RuleFor(x => x.Metadata!)
                .SetValidator(new UpdateDeceasedMetadataDtoValidator());
        });
    }
}