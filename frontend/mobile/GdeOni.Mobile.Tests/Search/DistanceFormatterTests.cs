using FluentAssertions;
using GdeOni.Mobile.Shared.Search;
using Xunit;

namespace GdeOni.Mobile.Tests.Search;

public class DistanceFormatterTests
{
    [Theory]
    [InlineData(0, "0 м")]
    [InlineData(7, "7 м")]
    [InlineData(120, "120 м")]
    [InlineData(999, "999 м")]
    public void Under_kilometer_uses_meters(int meters, string expected)
    {
        DistanceFormatter.Format(meters).Should().Be(expected);
    }

    [Theory]
    [InlineData(1000, "1 км")]
    [InlineData(1200, "1.2 км")]
    [InlineData(1234, "1.2 км")]      // округление к десятым
    [InlineData(4900, "4.9 км")]
    [InlineData(5000, "5 км")]
    public void At_or_above_kilometer_uses_kilometers_with_one_decimal(int meters, string expected)
    {
        DistanceFormatter.Format(meters).Should().Be(expected);
    }

    [Fact]
    public void Negative_input_treated_as_zero()
    {
        // Backend гарантирует не-отрицательное число, но guard на UI
        // от любого мусора — лучше "0 м" чем "-5 м" в интерфейсе.
        DistanceFormatter.Format(-5).Should().Be("0 м");
    }

    [Fact]
    public void Uses_invariant_culture_dot_not_locale_separator()
    {
        // Защита от ru-RU локали: десятичный разделитель должен быть
        // точка, иначе UI получит "1,2 км" — visual inconsistency с
        // координатами, которые мы везде форматируем через invariant.
        DistanceFormatter.Format(1200).Should().Contain(".").And.NotContain(",");
    }
}
