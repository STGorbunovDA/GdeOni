using FluentAssertions;
using GdeOni.Mobile.Shared.Utils;
using Xunit;

namespace GdeOni.Mobile.Tests.Utils;

public class CoordinateParserTests
{
    // ───────────────────── TryParseDouble: разделители и пробелы ─────────────────────

    [Theory]
    [InlineData("55.755826", 55.755826)]
    [InlineData("55,755826", 55.755826)]    // ru-RU запятая
    [InlineData(" 55.755826 ", 55.755826)]  // пробелы вокруг
    [InlineData("-12.345", -12.345)]
    [InlineData("0", 0.0)]
    [InlineData("100", 100.0)]
    public void TryParseDouble_accepts_dot_and_comma_and_trims_whitespace(string input, double expected)
    {
        var ok = CoordinateParser.TryParseDouble(input, out var value);

        ok.Should().BeTrue();
        value.Should().BeApproximately(expected, 0.000001);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not a number")]
    [InlineData("12.34.56")]
    public void TryParseDouble_returns_false_for_invalid_input(string? input)
    {
        var ok = CoordinateParser.TryParseDouble(input, out _);
        ok.Should().BeFalse();
    }

    // ───────────────────── Latitude: диапазон [-90, 90] ─────────────────────

    [Theory]
    [InlineData("0", true)]
    [InlineData("90", true)]
    [InlineData("-90", true)]
    [InlineData("55.755", true)]
    [InlineData("90.000001", false)]
    [InlineData("-90.000001", false)]
    [InlineData("180", false)]
    [InlineData("abc", false)]
    public void TryParseLatitude_validates_range(string input, bool expectedOk)
    {
        var ok = CoordinateParser.TryParseLatitude(input, out _);
        ok.Should().Be(expectedOk);
    }

    // ───────────────────── Longitude: диапазон [-180, 180] ─────────────────────

    [Theory]
    [InlineData("0", true)]
    [InlineData("180", true)]
    [InlineData("-180", true)]
    [InlineData("37.617300", true)]
    [InlineData("180.0001", false)]
    [InlineData("-180.0001", false)]
    [InlineData("xyz", false)]
    public void TryParseLongitude_validates_range(string input, bool expectedOk)
    {
        var ok = CoordinateParser.TryParseLongitude(input, out _);
        ok.Should().Be(expectedOk);
    }

    // ───────────────────── Accuracy: ≥ 0 ─────────────────────

    [Theory]
    [InlineData("0", true)]
    [InlineData("5", true)]
    [InlineData("100", true)]
    [InlineData("999999", true)]
    [InlineData("-1", false)]
    [InlineData("-0.5", false)]
    public void TryParseAccuracy_must_be_non_negative(string input, bool expectedOk)
    {
        var ok = CoordinateParser.TryParseAccuracy(input, out _);
        ok.Should().Be(expectedOk);
    }
}
