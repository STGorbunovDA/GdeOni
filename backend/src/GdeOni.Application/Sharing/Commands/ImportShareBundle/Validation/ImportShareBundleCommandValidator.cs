using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Sharing.Commands.ImportShareBundle.Model;
using GdeOni.Domain.Aggregates.Sharing;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Sharing.Commands.ImportShareBundle.Validation;

public sealed class ImportShareBundleCommandValidator
    : AbstractValidator<ImportShareBundleCommand>
{
    public ImportShareBundleCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithError(Errors.Share.NotFound())
            .MaximumLength(ShareBundle.MaxCodeLength)
            .WithError(Errors.Share.NotFound());
    }
}
