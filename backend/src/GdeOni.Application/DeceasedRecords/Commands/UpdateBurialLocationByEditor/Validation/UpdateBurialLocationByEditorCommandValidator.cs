using FluentValidation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateBurialLocationByEditor.Model;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateBurialLocationByEditor.Validation;

public sealed class UpdateBurialLocationByEditorCommandValidator : AbstractValidator<UpdateBurialLocationByEditorCommand>
{
    public UpdateBurialLocationByEditorCommandValidator()
    {
        RuleFor(x => x.DeceasedId).NotEmpty();
        // Lat/Lon должны быть либо оба заполнены, либо оба null.
        RuleFor(x => x).Must(c => c.Latitude.HasValue == c.Longitude.HasValue)
            .WithMessage("Latitude and Longitude must be both set or both null.");
    }
}
