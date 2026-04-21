using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.Validation;

public sealed class SetPrimaryPhotoCommandValidator : AbstractValidator<SetPrimaryPhotoCommand>
{
    public SetPrimaryPhotoCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.PhotoId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("photo_id"));
    }
}