using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Subscriptions.Queries.GetAdminPayments.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Queries.GetAdminPayments.Validation;

/// <summary>
/// D23. Валидация админ-запроса платежей. Помимо пагинации проверяет
/// диапазон дат: From не позже To. EmailSearch ограничиваем по длине
/// чтобы запрос с гигантским паттерном не упёрся в LIKE-tail.
/// </summary>
public sealed class GetAdminPaymentsQueryValidator : AbstractValidator<GetAdminPaymentsQuery>
{
    public GetAdminPaymentsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithError(Errors.Pagination.PageMustBeGreaterThanZero());

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithError(Errors.Pagination.PageSizeOutOfRange(1, 100));

        RuleFor(x => x.EmailSearch)
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.EmailSearch));

        RuleFor(x => x)
            .Must(q => q.CreatedFromUtc <= q.CreatedToUtc)
            .When(q => q.CreatedFromUtc.HasValue && q.CreatedToUtc.HasValue)
            .WithMessage("CreatedFromUtc must be earlier than or equal to CreatedToUtc.");
    }
}
