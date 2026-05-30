using FluentAssertions;
using GdeOni.Mobile.Shared.Notifications;
using Xunit;

namespace GdeOni.Mobile.Tests.Notifications;

public sealed class AnniversaryDateCalculatorTests
{
    [Fact]
    public void NextAnniversary_EventDateInFutureThisYear_ReturnsThisYear()
    {
        var today = new DateOnly(2026, 5, 20);
        var birth = new DateOnly(1980, 8, 10);

        AnniversaryDateCalculator.NextAnniversary(birth, today)
            .Should().Be(new DateOnly(2026, 8, 10));
    }

    [Fact]
    public void NextAnniversary_EventDatePastThisYear_ReturnsNextYear()
    {
        var today = new DateOnly(2026, 5, 20);
        var birth = new DateOnly(1980, 3, 15);

        AnniversaryDateCalculator.NextAnniversary(birth, today)
            .Should().Be(new DateOnly(2027, 3, 15));
    }

    [Fact]
    public void NextAnniversary_EventDateToday_ReturnsToday()
    {
        // Сегодняшняя годовщина — показываем сегодня (юзер должен
        // получить уведомление в тот же день).
        var today = new DateOnly(2026, 8, 10);
        var birth = new DateOnly(1980, 8, 10);

        AnniversaryDateCalculator.NextAnniversary(birth, today)
            .Should().Be(today);
    }

    [Fact]
    public void NextAnniversary_Feb29InLeapYear_ReturnsFeb29()
    {
        var today = new DateOnly(2028, 1, 1);    // 2028 — високосный
        var birth = new DateOnly(2000, 2, 29);

        AnniversaryDateCalculator.NextAnniversary(birth, today)
            .Should().Be(new DateOnly(2028, 2, 29));
    }

    [Fact]
    public void NextAnniversary_Feb29InNonLeapYear_FallsBackToFeb28()
    {
        var today = new DateOnly(2027, 1, 1);    // 2027 — невисокосный
        var birth = new DateOnly(2000, 2, 29);

        AnniversaryDateCalculator.NextAnniversary(birth, today)
            .Should().Be(new DateOnly(2027, 2, 28));
    }

    [Fact]
    public void NextAnniversary_Feb29AlreadyPastInNonLeapYear_NextYearMarchAdjusted()
    {
        // 1 марта 2027 — Feb29 в 2027 (фактически 28.02) уже прошёл,
        // следующая годовщина = 29 февраля 2028 (високосный).
        var today = new DateOnly(2027, 3, 1);
        var birth = new DateOnly(2000, 2, 29);

        AnniversaryDateCalculator.NextAnniversary(birth, today)
            .Should().Be(new DateOnly(2028, 2, 29));
    }

    [Fact]
    public void NextAnniversary_DecemberLast_ReturnsThisYearDec31()
    {
        var today = new DateOnly(2026, 12, 1);
        var birth = new DateOnly(1990, 12, 31);

        AnniversaryDateCalculator.NextAnniversary(birth, today)
            .Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public void NextAnniversary_JanuaryFirst_AlreadyAdvancesToNextYear()
    {
        var today = new DateOnly(2027, 1, 2);
        var birth = new DateOnly(1990, 1, 1);

        AnniversaryDateCalculator.NextAnniversary(birth, today)
            .Should().Be(new DateOnly(2028, 1, 1));
    }

    [Fact]
    public void NextAnniversaryAfter_EventDateToday_ReturnsNextYear()
    {
        // Защита от self-reschedule цикла: после срабатывания alarm'а сегодня
        // следующая годовщина должна быть СТРОГО на следующий год, иначе
        // AlarmManager.SetExactAndAllowWhileIdle с прошедшим timestamp
        // выстрелит сразу → бесконечный цикл уведомлений.
        var today = new DateOnly(2026, 8, 10);
        var birth = new DateOnly(1980, 8, 10);

        AnniversaryDateCalculator.NextAnniversaryAfter(birth, today)
            .Should().Be(new DateOnly(2027, 8, 10));
    }

    [Fact]
    public void NextAnniversaryAfter_EventDateInFutureThisYear_ReturnsThisYear()
    {
        // Если годовщина впереди — поведение совпадает с NextAnniversary.
        var today = new DateOnly(2026, 5, 20);
        var birth = new DateOnly(1980, 8, 10);

        AnniversaryDateCalculator.NextAnniversaryAfter(birth, today)
            .Should().Be(new DateOnly(2026, 8, 10));
    }
}
