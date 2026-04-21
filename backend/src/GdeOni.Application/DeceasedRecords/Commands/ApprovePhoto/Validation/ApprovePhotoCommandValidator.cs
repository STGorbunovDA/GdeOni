using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.ApprovePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ApprovePhoto.Validation;

public sealed class ApprovePhotoCommandValidator : AbstractValidator<ApprovePhotoCommand>
{
    public ApprovePhotoCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.PhotoId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("photo_id"));
    }
}