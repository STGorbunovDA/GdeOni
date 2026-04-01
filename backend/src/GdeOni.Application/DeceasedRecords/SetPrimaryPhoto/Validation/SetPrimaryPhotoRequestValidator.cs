using FluentValidation;
using GdeOni.Application.DeceasedRecords.SetPrimaryPhoto.Model;

namespace GdeOni.Application.DeceasedRecords.SetPrimaryPhoto.Validation;

public sealed class SetPrimaryPhotoRequestValidator : AbstractValidator<SetPrimaryPhotoRequest>
{
    public SetPrimaryPhotoRequestValidator()
    {
        RuleFor(x => x.DeceasedId).NotEmpty();
        RuleFor(x => x.PhotoId).NotEmpty();
    }
}