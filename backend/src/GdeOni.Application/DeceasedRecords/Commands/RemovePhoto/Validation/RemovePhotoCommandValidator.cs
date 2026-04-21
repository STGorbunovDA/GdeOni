using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.Validation;

public sealed class RemovePhotoCommandValidator : AbstractValidator<RemovePhotoCommand>
{
    public RemovePhotoCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.PhotoId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("photo_id"));
    }
}