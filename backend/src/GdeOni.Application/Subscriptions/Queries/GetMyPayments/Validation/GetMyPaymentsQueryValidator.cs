using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Queries.GetMyPayments.Validation;

/// <summary>
/// D23. Валидация пагинации платежей юзера. Симметрично остальным
/// query-валидаторам — 400 на неверные значения вместо тихого clamping
/// в use case'е (раньше Page=-5 молча приводился к 1, маскируя баг
/// клиента).
/// </summary>
public sealed class GetMyPaymentsQueryValidator : AbstractValidator<GetMyPaymentsQuery>
{
    public GetMyPaymentsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithError(Errors.Pagination.PageMustBeGreaterThanZero());

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithError(Errors.Pagination.PageSizeOutOfRange(1, 100));
    }
}
