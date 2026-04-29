using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.Validation;

public sealed class AddDeceasedAtGraveCommandValidator : AbstractValidator<AddDeceasedAtGraveCommand>
{
    public AddDeceasedAtGraveCommandValidator()
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

        RuleFor(x => x.GraveLocation)
            .NotNull()
            .WithError(Errors.General.ValueIsRequired("graveLocation"));

        RuleFor(x => x.GraveLocation)
            .SetValidator(new AddDeceasedAtGraveLocationCommandValidator())
            .When(x => x.GraveLocation is not null);

        RuleFor(x => x.Tracking)
            .NotNull()
            .WithError(Errors.General.ValueIsRequired("tracking"));

        RuleFor(x => x.Tracking)
            .SetValidator(new AddDeceasedAtGraveTrackingCommandValidator())
            .When(x => x.Tracking is not null);
    }
}

public sealed class AddDeceasedAtGraveLocationCommandValidator
    : AbstractValidator<AddDeceasedAtGraveLocationCommand>
{
    public AddDeceasedAtGraveLocationCommandValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithError(Errors.BurialLocation.LatitudeInvalid());

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithError(Errors.BurialLocation.LongitudeInvalid());

        RuleFor(x => x.AccuracyMeters)
            .GreaterThanOrEqualTo(0)
            .WithError(Errors.BurialLocation.AccuracyMetersInvalid())
            .When(x => x.AccuracyMeters.HasValue);

        RuleFor(x => x.Country)
            .MaximumLength(BurialLocation.MaxCountryLength)
            .WithError(Errors.BurialLocation.CountryTooLong(BurialLocation.MaxCountryLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Country));

        RuleFor(x => x.City)
            .MaximumLength(BurialLocation.MaxCityLength)
            .WithError(Errors.BurialLocation.CityTooLong(BurialLocation.MaxCityLength))
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.CemeteryName)
            .MaximumLength(BurialLocation.MaxCemeteryNameLength)
            .WithError(Errors.BurialLocation.CemeteryNameTooLong(BurialLocation.MaxCemeteryNameLength))
            .When(x => !string.IsNullOrWhiteSpace(x.CemeteryName));

        RuleFor(x => x.PlotNumber)
            .MaximumLength(BurialLocation.MaxPlotNumberLength)
            .WithError(Errors.BurialLocation.PlotNumberTooLong(BurialLocation.MaxPlotNumberLength))
            .When(x => !string.IsNullOrWhiteSpace(x.PlotNumber));

        RuleFor(x => x.GraveNumber)
            .MaximumLength(BurialLocation.MaxGraveNumberLength)
            .WithError(Errors.BurialLocation.GraveNumberTooLong(BurialLocation.MaxGraveNumberLength))
            .When(x => !string.IsNullOrWhiteSpace(x.GraveNumber));
    }
}

public sealed class AddDeceasedAtGraveTrackingCommandValidator
    : AbstractValidator<AddDeceasedAtGraveTrackingCommand>
{
    public AddDeceasedAtGraveTrackingCommandValidator()
    {
        RuleFor(x => x.RelationshipType)
            .IsInEnum()
            .WithError(Errors.Tracking.RelationshipTypeInvalid());
    }
}
