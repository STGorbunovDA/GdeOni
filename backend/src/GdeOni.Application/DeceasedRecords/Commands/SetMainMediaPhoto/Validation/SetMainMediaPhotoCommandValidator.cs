using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.Validation;

public sealed class SetMainMediaPhotoCommandValidator : AbstractValidator<SetMainMediaPhotoCommand>
{
    public SetMainMediaPhotoCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.MediaId)
            .NotEmpty()
            .WithError(Errors.DeceasedMedia.IdRequired());
    }
}
