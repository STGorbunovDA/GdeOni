using FluentValidation;
using GdeOni.Application.DeceasedRecords.Create.Model;

namespace GdeOni.Application.DeceasedRecords.Create.Validation;

public sealed class CreateDeceasedBurialLocationDtoValidator
    : AbstractValidator<CreateDeceasedBurialLocationDto>
{
    public CreateDeceasedBurialLocationDtoValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(200).WithMessage("Country must be at most 200 characters.");

        RuleFor(x => x.Region)
            .MaximumLength(200).WithMessage("Region must be at most 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Region));

        RuleFor(x => x.City)
            .MaximumLength(200).WithMessage("City must be at most 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.CemeteryName)
            .MaximumLength(300).WithMessage("Cemetery name must be at most 300 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.CemeteryName));

        RuleFor(x => x.PlotNumber)
            .MaximumLength(100).WithMessage("Plot number must be at most 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.PlotNumber));

        RuleFor(x => x.GraveNumber)
            .MaximumLength(100).WithMessage("Grave number must be at most 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.GraveNumber));
    }
}