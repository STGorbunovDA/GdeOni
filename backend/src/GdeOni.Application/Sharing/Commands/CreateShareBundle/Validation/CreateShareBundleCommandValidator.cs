using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Sharing.Commands.CreateShareBundle.Model;
using GdeOni.Domain.Aggregates.Sharing;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Sharing.Commands.CreateShareBundle.Validation;

public sealed class CreateShareBundleCommandValidator
    : AbstractValidator<CreateShareBundleCommand>
{
    public CreateShareBundleCommandValidator()
    {
        RuleFor(x => x.DeceasedIds)
            .NotEmpty()
            .WithError(Errors.Share.DeceasedIdsRequired())
            .Must(ids => ids is null || ids.Count <= ShareBundle.MaxItems)
            .WithError(Errors.Share.TooManyItems(ShareBundle.MaxItems));
    }
}
