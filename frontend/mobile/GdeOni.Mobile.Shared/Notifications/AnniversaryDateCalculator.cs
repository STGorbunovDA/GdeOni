namespace GdeOni.Mobile.Shared.Notifications;

/// <summary>
/// E23. Чистая логика "когда следующая годовщина после nowUtc". Учитывает:
/// - Если дата в этом году ещё не наступила или сегодня — возвращает её.
/// - Если уже прошла — следующий год.
/// - 29 февраля в невисокосный год → 28 февраля (Android AlarmManager
///   принимает любую DateTime, мы решаем безопасный fallback).
/// </summary>
public static class AnniversaryDateCalculator
{
    /// <summary>
    /// Считает ближайшую годовщину начиная с <paramref name="today"/> включительно.
    /// Используется при первичном scheduling — если дата сегодня и время
    /// ещё не наступило, alarm выстрелит сегодня.
    /// </summary>
    public static DateOnly NextAnniversary(DateOnly eventDate, DateOnly today)
    {
        var thisYearAnniversary = ToValidDateInYear(eventDate, today.Year);

        return thisYearAnniversary >= today
            ? thisYearAnniversary
            : ToValidDateInYear(eventDate, today.Year + 1);
    }

    /// <summary>
    /// Считает годовщину строго ПОСЛЕ <paramref name="today"/>. Используется
    /// при self-rescheduling в receiver'е: после того как alarm выстрелил
    /// сегодня, следующий должен быть на следующий год (а не снова сегодня —
    /// иначе бесконечный цикл уведомлений).
    /// </summary>
    public static DateOnly NextAnniversaryAfter(DateOnly eventDate, DateOnly today)
    {
        var thisYearAnniversary = ToValidDateInYear(eventDate, today.Year);

        return thisYearAnniversary > today
            ? thisYearAnniversary
            : ToValidDateInYear(eventDate, today.Year + 1);
    }

    /// <summary>
    /// Маппит (day, month) события в конкретный год. 29 февраля в
    /// невисокосный год → 28 февраля.
    /// </summary>
    private static DateOnly ToValidDateInYear(DateOnly eventDate, int year)
    {
        if (eventDate.Month == 2 && eventDate.Day == 29 && !DateTime.IsLeapYear(year))
            return new DateOnly(year, 2, 28);

        return new DateOnly(year, eventDate.Month, eventDate.Day);
    }
}
