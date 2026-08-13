using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.ResendConfirmation.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.ResendConfirmation.Validation;

public sealed class ResendEmailConfirmationCommandValidator
    : AbstractValidator<ResendEmailConfirmationCommand>
{
    public ResendEmailConfirmationCommandValidator()
    {
        // Принимаем email ИЛИ логин (см. use case): на гейт «подтвердите
        // email» приезжают и со входа по логину. EmailAddress() здесь отбивал
        // бы такой запрос ещё до поиска пользователя.
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithError(Errors.User.EmailRequired())
            .MaximumLength(User.MaxEmailLength)
            .WithError(Errors.User.EmailTooLong(User.MaxEmailLength));
    }
}
