using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Create.Validation;

public sealed class CreateDeceasedCommandValidator : AbstractValidator<CreateDeceasedCommand>
{
    public CreateDeceasedCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithError(Errors.PersonName.FirstNameRequired())
            .MaximumLength(PersonName.MaxFirstName)
            .WithError(Errors.PersonName.FirstNameTooLong(PersonName.MaxFirstName));

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithError(Errors.PersonName.LastNameRequired())
            .MaximumLength(PersonName.MaxLastName)
            .WithError(Errors.PersonName.LastNameTooLong(PersonName.MaxLastName));

        RuleFor(x => x.MiddleName)
            .MaximumLength(PersonName.MaxMiddleName)
            .WithError(Errors.PersonName.MiddleNameTooLong(PersonName.MaxMiddleName))
            .When(x => !string.IsNullOrWhiteSpace(x.MiddleName));

        RuleFor(x => x.DeathDate)
            .Must(x => x != default)
            .WithError(Errors.LifePeriod.DeathDateRequired());

        RuleFor(x => x.DeathDate)
            .Must(x => x <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithError(Errors.LifePeriod.DeathDateInFuture());

        RuleFor(x => x)
            .Must(x => x.BirthDate is null || x.BirthDate.Value <= x.DeathDate)
            .WithError(Errors.LifePeriod.BirthDateAfterDeathDate());

        RuleFor(x => x.ShortDescription)
            .MaximumLength(Deceased.MaxShortDescriptionLength)
            .WithError(Errors.Deceased.ShortDescriptionTooLong(Deceased.MaxShortDescriptionLength))
            .When(x => !string.IsNullOrWhiteSpace(x.ShortDescription));

        RuleFor(x => x.Biography)
            .MaximumLength(Deceased.MaxBiographyLength)
            .WithError(Errors.Deceased.BiographyTooLong(Deceased.MaxBiographyLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Biography));

        RuleFor(x => x.BurialLocation!)
            .SetValidator(new CreateDeceasedBurialLocationCommandValidator())
            .When(x => x.BurialLocation is not null);

        RuleForEach(x => x.Photos!)
            .SetValidator(new CreateDeceasedPhotoCommandValidator())
            .When(x => x.Photos is not null);

        RuleForEach(x => x.Memories!)
            .SetValidator(new CreateDeceasedMemoryCommandValidator())
            .When(x => x.Memories is not null);

        RuleFor(x => x.Metadata!)
            .SetValidator(new CreateDeceasedMetadataCommandValidator())
            .When(x => x.Metadata is not null);
    }
}