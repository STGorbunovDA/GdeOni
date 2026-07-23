using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Events.Queries.GetHolidays.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Queries.GetHolidays.Validation;

public sealed class GetHolidaysQueryValidator : AbstractValidator<GetHolidaysQuery>
{
    /// <summary>Максимум дней в одном запросе — защита от «дай мне 50 лет».</summary>
    private const int MaxRangeDays = 366;

    public GetHolidaysQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .WithError(Errors.General.ValueIsInvalid("to"));

        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber <= MaxRangeDays)
            .WithError(Errors.General.ValueIsInvalid("range"));
    }
}
