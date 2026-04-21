using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Create.Validation;

public sealed class CreateDeceasedCommandValidator : AbstractValidator<CreateDeceasedCommand>
{
    public CreateDeceasedCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithError(Errors.PersonName.FirstNameRequired())
            .MaximumLength(100)
            .WithError(Errors.PersonName.FirstNameTooLong(100));

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithError(Errors.PersonName.LastNameRequired())
            .MaximumLength(100)
            .WithError(Errors.PersonName.LastNameTooLong(100));

        RuleFor(x => x.MiddleName)
            .MaximumLength(100)
            .WithError(Errors.PersonName.MiddleNameTooLong(100))
            .When(x => !string.IsNullOrWhiteSpace(x.MiddleName));

        RuleFor(x => x.DeathDate)
            .NotEmpty()
            .WithError(Errors.LifePeriod.DeathDateRequired());

        RuleFor(x => x.DeathDate)
            .Must(x => x.Date <= DateTime.UtcNow.Date)
            .WithError(Errors.LifePeriod.DeathDateInFuture());

        RuleFor(x => x)
            .Must(x => x.BirthDate is null || x.BirthDate.Value.Date <= x.DeathDate.Date)
            .WithError(Errors.LifePeriod.BirthDateAfterDeathDate());

        RuleFor(x => x.ShortDescription)
            .MaximumLength(1000)
            .WithError(Errors.Deceased.ShortDescriptionTooLong(1000))
            .When(x => !string.IsNullOrWhiteSpace(x.ShortDescription));

        RuleFor(x => x.Biography)
            .MaximumLength(10000)
            .WithError(Errors.Deceased.BiographyTooLong(10000))
            .When(x => !string.IsNullOrWhiteSpace(x.Biography));

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty()
            .WithError(Errors.Deceased.CreatedByRequired());

        RuleFor(x => x.BurialLocation)
            .NotNull()
            .WithError(Errors.Deceased.BurialLocationRequired());

        When(x => x.BurialLocation is not null, () =>
        {
            RuleFor(x => x.BurialLocation!)
                .SetValidator(new CreateDeceasedBurialLocationCommandValidator());
        });

        RuleForEach(x => x.Photos)
            .SetValidator(new CreateDeceasedPhotoCommandValidator());

        RuleForEach(x => x.Memories)
            .SetValidator(new CreateDeceasedMemoryCommandValidator());

        When(x => x.Metadata is not null, () =>
        {
            RuleFor(x => x.Metadata!)
                .SetValidator(new CreateDeceasedMetadataCommandValidator());
        });
    }
}