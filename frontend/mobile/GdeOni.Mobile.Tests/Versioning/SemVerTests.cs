using FluentAssertions;
using GdeOni.Mobile.Shared.Versioning;
using Xunit;

namespace GdeOni.Mobile.Tests.Versioning;

public sealed class SemVerTests
{
    [Theory]
    [InlineData("1.0.0", 1, 0, 0)]
    [InlineData("0.0.1", 0, 0, 1)]
    [InlineData("10.20.30", 10, 20, 30)]
    [InlineData("0.0.0", 0, 0, 0)]
    public void TryParse_ValidString_ReturnsParsedSemVer(string input, int major, int minor, int patch)
    {
        SemVer.TryParse(input, out var version).Should().BeTrue();
        version.Should().Be(new SemVer(major, minor, patch));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("1.0")]
    [InlineData("1.0.0.0")]
    [InlineData("1.0.0-rc")]
    [InlineData("v1.0.0")]
    [InlineData("a.b.c")]
    [InlineData("-1.0.0")]
    [InlineData("1.-1.0")]
    public void TryParse_InvalidString_ReturnsFalse(string? input)
    {
        SemVer.TryParse(input, out _).Should().BeFalse();
    }

    [Fact]
    public void Parse_InvalidString_ThrowsFormatException()
    {
        Action act = () => SemVer.Parse("garbage");
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0", true)]   // ==  → AtLeast true
    [InlineData("1.0.1", "1.0.0", true)]   // patch bump
    [InlineData("1.1.0", "1.0.9", true)]   // minor bump побеждает большее patch
    [InlineData("2.0.0", "1.99.99", true)] // major bump побеждает всё
    [InlineData("1.0.0", "1.0.1", false)]  // строго меньше
    [InlineData("1.0.0", "1.1.0", false)]
    [InlineData("1.0.0", "2.0.0", false)]
    public void IsAtLeast_OrdersByMajorMinorPatch(string current, string minimum, bool expected)
    {
        SemVer.Parse(current).IsAtLeast(SemVer.Parse(minimum)).Should().Be(expected);
    }

    [Fact]
    public void Operators_FollowSemanticOrdering()
    {
        var a = SemVer.Parse("1.2.3");
        var b = SemVer.Parse("1.2.4");
        var c = SemVer.Parse("1.2.3");

        (a < b).Should().BeTrue();
        (a > b).Should().BeFalse();
        (a <= c).Should().BeTrue();
        (a >= c).Should().BeTrue();
        a.Should().Be(c);
    }

    [Fact]
    public void ToString_ReturnsCanonicalFormat()
    {
        new SemVer(1, 2, 3).ToString().Should().Be("1.2.3");
    }
}
