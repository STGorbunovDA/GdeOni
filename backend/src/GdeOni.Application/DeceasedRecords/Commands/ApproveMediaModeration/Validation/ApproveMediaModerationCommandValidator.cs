using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMediaModeration.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ApproveMediaModeration.Validation;

public sealed class ApproveMediaModerationCommandValidator
    : AbstractValidator<ApproveMediaModerationCommand>
{
    public ApproveMediaModerationCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.MediaId)
            .NotEmpty()
            .WithError(Errors.DeceasedMedia.IdRequired());
    }
}
