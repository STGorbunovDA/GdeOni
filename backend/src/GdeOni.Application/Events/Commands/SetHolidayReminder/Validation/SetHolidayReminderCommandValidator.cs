using FluentValidation;
using GdeOni.Application.Events.Commands.SetHolidayReminder.Model;
using GdeOni.Domain.Aggregates.Events;

namespace GdeOni.Application.Events.Commands.SetHolidayReminder.Validation;

public sealed class SetHolidayReminderCommandValidator
    : AbstractValidator<SetHolidayReminderCommand>
{
    /// <summary>Допустимые «за сколько дней»: 0 = в день, 1, 3, 7.</summary>
    private static readonly int[] AllowedLeadDays = { 0, 1, 3, 7 };

    public SetHolidayReminderCommandValidator()
    {
        RuleFor(x => x.HolidayKey)
            .NotEmpty().WithMessage("Не указан праздник.")
            .MaximumLength(HolidayReminder.MaxHolidayKeyLength);

        RuleFor(x => x.LeadDays)
            .NotNull().WithMessage("Не указан набор напоминаний.");

        RuleForEach(x => x.LeadDays)
            .Must(d => AllowedLeadDays.Contains(d))
            .WithMessage("Недопустимое значение напоминания (можно 0, 1, 3, 7 дней).");
    }
}
