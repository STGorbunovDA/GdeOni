using GdeOni.Application.Anniversaries;

namespace GdeOni.Application.Tests.Anniversaries;

/// <summary>
/// D37. Тесты чистой логики «наступает ли сегодня годовщина + число лет».
/// </summary>
public sealed class AnniversaryOccurrenceTests
{
    [Theory]
    // Точное совпадение дня/месяца, событие в прошлом → годовщина + годы.
    [InlineData(2000, 7, 11, 2026, 7, 11, true, 26)]
    [InlineData(1950, 1, 1, 2026, 1, 1, true, 76)]
    // Не тот день → не годовщина.
    [InlineData(2000, 7, 11, 2026, 7, 12, false, 0)]
    [InlineData(2000, 7, 11, 2026, 8, 11, false, 0)]
    // Годовщина «0 лет» (событие в этом же году) → не считается.
    [InlineData(2026, 7, 11, 2026, 7, 11, false, 0)]
    // 29 февраля в невисокосный год отмечаем 28-го.
    [InlineData(2000, 2, 29, 2025, 2, 28, true, 25)]
    // 28 февраля в невисокосный год — обычная дата, не «подменяет» 29-е
    // задним числом для события 28-го.
    [InlineData(2000, 2, 28, 2025, 2, 28, true, 25)]
    // В високосный год 28 февраля НЕ является годовщиной 29-го.
    [InlineData(2000, 2, 29, 2024, 2, 28, false, 0)]
    // В високосный год 29 февраля — прямое совпадение.
    [InlineData(2000, 2, 29, 2024, 2, 29, true, 24)]
    public void TryGet_ReturnsExpected(
        int ey, int em, int ed,
        int ty, int tm, int td,
        bool expectedMatch, int expectedYears)
    {
        var eventDate = new DateOnly(ey, em, ed);
        var today = new DateOnly(ty, tm, td);

        var matched = AnniversaryOccurrence.TryGet(eventDate, today, out var years);

        matched.Should().Be(expectedMatch);
        years.Should().Be(expectedYears);
    }
}
