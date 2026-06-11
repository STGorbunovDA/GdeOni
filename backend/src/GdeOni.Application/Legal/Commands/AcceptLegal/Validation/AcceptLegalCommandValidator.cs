using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Legal.Commands.AcceptLegal.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Legal.Commands.AcceptLegal.Validation;

public sealed class AcceptLegalCommandValidator : AbstractValidator<AcceptLegalCommand>
{
    public AcceptLegalCommandValidator()
    {
        RuleFor(x => x.PrivacyPolicyVersion)
            .GreaterThanOrEqualTo(1)
            .WithError(Errors.Legal.PrivacyPolicyVersionInvalid());

        RuleFor(x => x.TermsVersion)
            .GreaterThanOrEqualTo(1)
            .WithError(Errors.Legal.TermsVersionInvalid());
    }
}
