using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Update.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Update.Validation;

public sealed class UpdateDeceasedCommandValidator : AbstractValidator<UpdateDeceasedCommand>
{
    public UpdateDeceasedCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

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
            .NotEmpty()
            .WithError(Errors.LifePeriod.DeathDateRequired());

        RuleFor(x => x.ShortDescription)
            .MaximumLength(Deceased.MaxShortDescriptionLength)
            .WithError(Errors.Deceased.ShortDescriptionTooLong(Deceased.MaxShortDescriptionLength))
            .When(x => !string.IsNullOrWhiteSpace(x.ShortDescription));

        RuleFor(x => x.Biography)
            .MaximumLength(Deceased.MaxBiographyLength)
            .WithError(Errors.Deceased.BiographyTooLong(Deceased.MaxBiographyLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Biography));

        RuleFor(x => x.BirthDate)
            .LessThanOrEqualTo(x => x.DeathDate)
            .WithError(Errors.LifePeriod.BirthDateAfterDeathDate())
            .When(x => x.BirthDate.HasValue);

        RuleFor(x => x.BurialLocation!)
            .SetValidator(new UpdateDeceasedBurialLocationCommandValidator())
            .When(x => x.BurialLocation is not null);

        RuleFor(x => x.Metadata!)
            .SetValidator(new UpdateDeceasedMetadataCommandValidator())
            .When(x => x.Metadata is not null);
    }
}