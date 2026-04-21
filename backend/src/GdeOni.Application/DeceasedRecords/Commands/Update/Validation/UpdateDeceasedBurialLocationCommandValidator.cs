using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Update.Model;
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
            .MaximumLength(200)
            .WithError(Errors.BurialLocation.CountryTooLong(200));

        RuleFor(x => x.Region)
            .MaximumLength(200)
            .WithError(Errors.BurialLocation.RegionTooLong(200))
            .When(x => !string.IsNullOrWhiteSpace(x.Region));

        RuleFor(x => x.City)
            .MaximumLength(200)
            .WithError(Errors.BurialLocation.CityTooLong(200))
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.CemeteryName)
            .MaximumLength(300)
            .WithError(Errors.BurialLocation.CemeteryNameTooLong(300))
            .When(x => !string.IsNullOrWhiteSpace(x.CemeteryName));

        RuleFor(x => x.PlotNumber)
            .MaximumLength(100)
            .WithError(Errors.BurialLocation.PlotNumberTooLong(100))
            .When(x => !string.IsNullOrWhiteSpace(x.PlotNumber));

        RuleFor(x => x.GraveNumber)
            .MaximumLength(100)
            .WithError(Errors.BurialLocation.GraveNumberTooLong(100))
            .When(x => !string.IsNullOrWhiteSpace(x.GraveNumber));

        RuleFor(x => x.Accuracy)
            .IsInEnum()
            .WithError(Errors.BurialLocation.AccuracyInvalid());
    }
}