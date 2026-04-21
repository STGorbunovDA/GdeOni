using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Update.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Update.Validation;

public sealed class UpdateDeceasedBurialLocationCommandValidator
    : AbstractValidator<UpdateDeceasedBurialLocationCommand>
{
    public UpdateDeceasedBurialLocationCommandValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithError(Errors.BurialLocation.LatitudeInvalid());

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithError(Errors.BurialLocation.LongitudeInvalid());

        RuleFor(x => x.Country)
            .NotEmpty()
            .WithError(Errors.BurialLocation.CountryRequired())
            .MaximumLength(BurialLocation.MaxCountryLength)
            .WithError(Errors.BurialLocation.CountryTooLong(BurialLocation.MaxCountryLength));

        RuleFor(x => x.Region)
            .MaximumLength(BurialLocation.MaxRegionLength)
            .WithError(Errors.BurialLocation.RegionTooLong(BurialLocation.MaxRegionLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Region));

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

        RuleFor(x => x.Accuracy)
            .IsInEnum()
            .WithError(Errors.BurialLocation.AccuracyInvalid());
    }
}