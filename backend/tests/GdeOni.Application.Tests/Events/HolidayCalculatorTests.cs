using GdeOni.Application.Events;

namespace GdeOni.Application.Tests.Events;

/// <summary>
/// Тесты календаря праздников: подвижные даты (Пасха и производные),
/// фиксированные, фильтрация диапазона.
/// </summary>
public sealed class HolidayCalculatorTests
{
    [Theory]
    [InlineData(2024, 5, 5)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 12)]
    [InlineData(2027, 5, 2)]
    public void OrthodoxEaster_KnownYears(int year, int m, int d)
    {
        HolidayCalculator.OrthodoxEaster(year).Should().Be(new DateOnly(year, m, d));
    }

    [Fact]
    public void GetHolidays_Radonitsa2026_IsNinthDayAfterEaster()
    {
        var holidays = HolidayCalculator.GetHolidays(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        var radonitsa = holidays.Single(h => h.Name == "Радоница");
        radonitsa.Date.Should().Be(new DateOnly(2026, 4, 21));
        radonitsa.Category.Should().Be(HolidayCategory.Memorial);
    }

    [Fact]
    public void GetHolidays_Trinity2026_Is49DaysAfterEaster()
    {
        var holidays = HolidayCalculator.GetHolidays(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        holidays.Single(h => h.Name.StartsWith("Троица")).Date
            .Should().Be(new DateOnly(2026, 5, 31));
    }

    [Fact]
    public void GetHolidays_AllParentalSaturdays_AreSaturdays()
    {
        var holidays = HolidayCalculator.GetHolidays(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        holidays
            .Where(h => h.Category == HolidayCategory.Memorial && h.Name.Contains("суббота"))
            .Should()
            .OnlyContain(h => h.Date.DayOfWeek == DayOfWeek.Saturday);
    }

    [Fact]
    public void GetHolidays_ContainsFixedAndMuslimAndState()
    {
        var holidays = HolidayCalculator.GetHolidays(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        holidays.Should().Contain(h =>
            h.Category == HolidayCategory.Orthodox && h.Date == new DateOnly(2026, 1, 7));
        holidays.Should().Contain(h =>
            h.Category == HolidayCategory.State && h.Date == new DateOnly(2026, 5, 9));
        // Мусульманские считаются таблично — точную дату не проверяем,
        // но за год их должно быть несколько, и все внутри года.
        holidays.Where(h => h.Category == HolidayCategory.Muslim)
            .Should().HaveCountGreaterThan(0)
            .And.OnlyContain(h => h.Date.Year == 2026);
    }

    /// <summary>
    /// Регресс: сначала в календаре были только двунадесятые праздники,
    /// и 12 июля (Петров день) на вкладке «События» не показывался вовсе.
    /// Проверяем все великие недвунадесятые — они непереходящие, поэтому
    /// даты фиксированные в любом году.
    /// </summary>
    [Theory]
    [InlineData(1, 14, "Обрезание")]
    [InlineData(7, 7, "Рождество Иоанна Предтечи")]
    [InlineData(7, 12, "Петра и Павла")]
    [InlineData(9, 11, "Усекновение")]
    [InlineData(10, 14, "Покров")]
    public void GetHolidays_ContainsGreatNonTwelveFeasts(int month, int day, string namePart)
    {
        var date = new DateOnly(2026, month, day);

        var holidays = HolidayCalculator.GetHolidays(date, date);

        holidays.Should().Contain(h =>
            h.Category == HolidayCategory.Orthodox && h.Name.Contains(namePart));
    }

    /// <summary>
    /// Светские дни поминовения попадают в Memorial, а не в State —
    /// в сервисе про захоронения им место рядом с родительскими субботами.
    /// </summary>
    [Fact]
    public void GetHolidays_SecularMemorialDays_AreMemorialCategory()
    {
        var holidays = HolidayCalculator.GetHolidays(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        holidays.Should().Contain(h =>
            h.Category == HolidayCategory.Memorial && h.Date == new DateOnly(2026, 6, 22));
        holidays.Should().Contain(h =>
            h.Category == HolidayCategory.Memorial && h.Date == new DateOnly(2026, 10, 30));
    }

    /// <summary>
    /// Переходящие даты сверены с православным календарём на 2026 год
    /// (Пасха — 12 апреля). Тест ловит ошибку в смещении от Пасхи: раньше
    /// «Начало Масленицы» стояло на E−56 и попадало на воскресенье, хотя
    /// сырная седмица начинается в понедельник (E−55).
    /// </summary>
    [Theory]
    [InlineData(2, 16, "Начало Масленицы (Сырная седмица)")]
    [InlineData(2, 22, "Прощёное воскресенье")]
    [InlineData(2, 23, "Начало Великого поста")]
    [InlineData(3, 1, "Торжество Православия")]
    [InlineData(4, 5, "Вход Господень в Иерусалим (Вербное воскресенье)")]
    [InlineData(4, 11, "Великая суббота")]
    [InlineData(4, 12, "Пасха (Светлое Христово Воскресение)")]
    [InlineData(4, 21, "Радоница")]
    [InlineData(5, 21, "Вознесение Господне")]
    [InlineData(5, 30, "Троицкая родительская суббота")]
    [InlineData(5, 31, "Троица (Пятидесятница)")]
    [InlineData(6, 8, "Начало Петрова поста")]
    public void GetHolidays_MovableDates2026_MatchOfficialCalendar(
        int month, int day, string name)
    {
        var date = new DateOnly(2026, month, day);

        var holidays = HolidayCalculator.GetHolidays(date, date);

        holidays.Should().Contain(h => h.Name == name);
    }

    /// <summary>
    /// Петров пост всегда заканчивается 11 июля, накануне Петра и Павла,
    /// а начинается в понедельник после Недели всех святых — значит его
    /// начало обязано быть понедельником в любом году.
    /// </summary>
    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    [InlineData(2027)]
    [InlineData(2030)]
    public void GetHolidays_PetrovFast_StartsOnMonday(int year)
    {
        var holidays = HolidayCalculator.GetHolidays(
            new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));

        var start = holidays.Single(h => h.Name == "Начало Петрова поста");

        start.Category.Should().Be(HolidayCategory.Fast);
        start.Date.DayOfWeek.Should().Be(DayOfWeek.Monday);
        start.Date.Should().BeBefore(new DateOnly(year, 7, 12));
    }

    [Fact]
    public void GetHolidays_FiltersToRange_AndSorts()
    {
        var from = new DateOnly(2026, 4, 12);
        var to = new DateOnly(2026, 4, 21);

        var holidays = HolidayCalculator.GetHolidays(from, to);

        holidays.Should().OnlyContain(h => h.Date >= from && h.Date <= to);
        holidays.Should().BeInAscendingOrder(h => h.Date);
        // В диапазон попадают Пасха (12-е) и Радоница (21-е).
        holidays.Should().Contain(h => h.Name.StartsWith("Пасха"));
        holidays.Should().Contain(h => h.Name == "Радоница");
    }

    [Fact]
    public void GetHolidays_EmptyWhenToBeforeFrom()
    {
        HolidayCalculator.GetHolidays(new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1))
            .Should().BeEmpty();
    }
}
