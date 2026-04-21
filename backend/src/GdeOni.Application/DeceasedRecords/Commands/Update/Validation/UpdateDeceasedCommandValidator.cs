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
            .MaximumLength(200)
            .WithError(Errors.PersonName.FirstNameTooLong(200));

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithError(Errors.PersonName.LastNameRequired())
            .MaximumLength(200)
            .WithError(Errors.PersonName.LastNameTooLong(200));

        RuleFor(x => x.MiddleName)
            .MaximumLength(200)
            .WithError(Errors.PersonName.MiddleNameTooLong(200))
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

        RuleFor(x => x.BurialLocation)
            .NotNull()
            .WithError(Errors.Deceased.BurialLocationRequired());

        RuleFor(x => x.BurialLocation!)
            .SetValidator(new UpdateDeceasedBurialLocationCommandValidator())
            .When(x => x.BurialLocation is not null);

        RuleFor(x => x.Metadata!)
            .SetValidator(new UpdateDeceasedMetadataCommandValidator())
            .When(x => x.Metadata is not null);
    }
}