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

        RuleFor(x => x.DeathDate)
            .Must(x => x.Date <= DateTime.UtcNow.Date)
            .WithError(Errors.LifePeriod.DeathDateInFuture());

        RuleFor(x => x)
            .Must(x => x.BirthDate is null || x.BirthDate.Value.Date <= x.DeathDate.Date)
            .WithError(Errors.LifePeriod.BirthDateAfterDeathDate());

        RuleFor(x => x.ShortDescription)
            .MaximumLength(Deceased.MaxShortDescriptionLength)
            .WithError(Errors.Deceased.ShortDescriptionTooLong(Deceased.MaxShortDescriptionLength))
            .When(x => !string.IsNullOrWhiteSpace(x.ShortDescription));

        RuleFor(x => x.Biography)
            .MaximumLength(Deceased.MaxBiographyLength)
            .WithError(Errors.Deceased.BiographyTooLong(Deceased.MaxBiographyLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Biography));

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty()
            .WithError(Errors.Deceased.CreatedByRequired());

        RuleFor(x => x.BurialLocation)
            .NotNull()
            .WithError(Errors.Deceased.BurialLocationRequired());

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