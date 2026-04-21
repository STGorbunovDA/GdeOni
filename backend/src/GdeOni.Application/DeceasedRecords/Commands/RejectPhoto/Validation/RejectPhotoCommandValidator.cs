using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.Validation;

public sealed class RejectPhotoCommandValidator : AbstractValidator<RejectPhotoCommand>
{
    public RejectPhotoCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.PhotoId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("photo_id"));
    }
}