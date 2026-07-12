namespace GdeOni.Application.Anniversaries;

/// <summary>
/// D37. Чистая логика «наступает ли сегодня годовщина события и сколько
/// лет исполняется». Backend-зеркало
/// <c>GdeOni.Mobile.Shared.Notifications.AnniversaryDateCalculator</c>:
/// та же трактовка 29 февраля, но здесь нас интересует не «когда
/// следующая», а «совпадает ли сегодня» + число прошедших лет.
///
/// Правило 29 февраля: в невисокосный год годовщину события 29.02
/// отмечаем 28 февраля (иначе она «выпадала» бы раз в 4 года).
/// </summary>
public static class AnniversaryOccurrence
{
    /// <summary>
    /// Возвращает true, если <paramref name="today"/> — годовщина
    /// <paramref name="eventDate"/>, и выставляет
    /// <paramref name="yearsSince"/> = число полных лет с события
    /// (1, 2, 3...). Требует, чтобы событие было в прошлом относительно
    /// сегодня (yearsSince ≥ 1) — годовщина «0 лет» не бывает.
    /// </summary>
    public static bool TryGet(DateOnly eventDate, DateOnly today, out int yearsSince)
    {
        yearsSince = 0;

        if (!IsAnniversaryDay(eventDate, today))
            return false;

        var years = today.Year - eventDate.Year;
        if (years < 1)
            return false;

        yearsSince = years;
        return true;
    }

    /// <summary>
    /// Совпадает ли календарный день (без учёта года), с поправкой на
    /// 29 февраля в невисокосный год.
    /// </summary>
    private static bool IsAnniversaryDay(DateOnly eventDate, DateOnly today)
    {
        if (eventDate.Month == today.Month && eventDate.Day == today.Day)
            return true;

        // 29 февраля в невисокосный год отмечаем 28 февраля.
        if (eventDate is { Month: 2, Day: 29 }
            && today is { Month: 2, Day: 28 }
            && !DateTime.IsLeapYear(today.Year))
        {
            return true;
        }

        return false;
    }
}
