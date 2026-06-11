using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.Block.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Block.Validation;

public sealed class BlockUserCommandValidator : AbstractValidator<BlockUserCommand>
{
    public BlockUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(Errors.User.IdRequired());

        // Длина reason также проверяется в User.Block — валидатор даёт
        // ранний "форма" feedback (UI получит 400 а не вынырнет до домена).
        RuleFor(x => x.Reason)
            .MaximumLength(User.MaxBlockReasonLength)
            .WithError(Errors.User.BlockReasonTooLong(User.MaxBlockReasonLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
