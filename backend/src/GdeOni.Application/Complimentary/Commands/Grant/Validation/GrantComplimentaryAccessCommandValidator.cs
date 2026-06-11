using FluentValidation;
using GdeOni.Application.Complimentary.Commands.Grant.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Complimentary.Commands.Grant.Validation;

public sealed class GrantComplimentaryAccessCommandValidator
    : AbstractValidator<GrantComplimentaryAccessCommand>
{
    public GrantComplimentaryAccessCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(Errors.User.IdRequired().Code)
            .WithMessage(Errors.User.IdRequired().Message);

        When(x => x.Note is not null, () =>
        {
            RuleFor(x => x.Note!)
                .MaximumLength(User.MaxComplimentaryNoteLength)
                .WithErrorCode(Errors.Complimentary.NoteTooLong(User.MaxComplimentaryNoteLength).Code)
                .WithMessage(Errors.Complimentary.NoteTooLong(User.MaxComplimentaryNoteLength).Message);
        });
    }
}
