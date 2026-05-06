using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.Validation;

public sealed class RejectMediaModerationCommandValidator
    : AbstractValidator<RejectMediaModerationCommand>
{
    public RejectMediaModerationCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.MediaId)
            .NotEmpty()
            .WithError(Errors.DeceasedMedia.IdRequired());
    }
}
