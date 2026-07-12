using GdeOni.Application.Common.Shared;

namespace GdeOni.Application.Tests.Common;

/// <summary>
/// D37. Русская плюрализация «год / года / лет».
/// </summary>
public sealed class RussianPluralTests
{
    [Theory]
    [InlineData(1, "год")]
    [InlineData(2, "года")]
    [InlineData(3, "года")]
    [InlineData(4, "года")]
    [InlineData(5, "лет")]
    [InlineData(11, "лет")]
    [InlineData(12, "лет")]
    [InlineData(14, "лет")]
    [InlineData(21, "год")]
    [InlineData(22, "года")]
    [InlineData(25, "лет")]
    [InlineData(100, "лет")]
    [InlineData(101, "год")]
    [InlineData(112, "лет")]
    public void Years_ReturnsCorrectForm(int count, string expected)
    {
        RussianPlural.Years(count).Should().Be(expected);
    }
}
