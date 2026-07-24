using FluentAssertions;
using GdeOni.Mobile.Shared.Theming;
using Xunit;

namespace GdeOni.Mobile.Tests.Theming;

public class ThemeModeParserTests
{
    [Theory]
    [InlineData("light", ThemeMode.Light)]
    [InlineData("dark", ThemeMode.Dark)]
    [InlineData("auto", ThemeMode.System)]
    public void Parse_maps_known_values(string stored, ThemeMode expected)
    {
        ThemeModeParser.Parse(stored).Should().Be(expected);
    }

    [Theory]
    [InlineData("  Light  ")]
    [InlineData("DARK")]
    [InlineData("Dark")]
    public void Parse_ignores_case_and_whitespace(string stored)
    {
        // Именно эти три должны распознаться как не-System.
        ThemeModeParser.Parse(stored).Should().NotBe(ThemeMode.System);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("system")]
    [InlineData("qwerty")]
    public void Parse_falls_back_to_System_for_unknown_or_empty(string? stored)
    {
        ThemeModeParser.Parse(stored).Should().Be(ThemeMode.System);
    }

    [Theory]
    [InlineData(ThemeMode.Light, "light")]
    [InlineData(ThemeMode.Dark, "dark")]
    [InlineData(ThemeMode.System, "auto")]
    public void ToStorageString_matches_web_values(ThemeMode mode, string expected)
    {
        ThemeModeParser.ToStorageString(mode).Should().Be(expected);
    }

    [Theory]
    [InlineData(ThemeMode.System)]
    [InlineData(ThemeMode.Light)]
    [InlineData(ThemeMode.Dark)]
    public void Roundtrip_storage_string_is_stable(ThemeMode mode)
    {
        var roundtripped = ThemeModeParser.Parse(ThemeModeParser.ToStorageString(mode));
        roundtripped.Should().Be(mode);
    }
}
