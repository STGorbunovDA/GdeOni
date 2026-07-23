using FluentAssertions;
using GdeOni.Mobile.Shared.Support;
using Xunit;

namespace GdeOni.Mobile.Tests.Support;

public class SupportDeceasedRefParserTests
{
    [Fact]
    public void TryExtract_NullDescription_ReturnsNull()
    {
        SupportDeceasedRefParser.TryExtract(null).Should().BeNull();
    }

    [Fact]
    public void TryExtract_EmptyDescription_ReturnsNull()
    {
        SupportDeceasedRefParser.TryExtract("").Should().BeNull();
        SupportDeceasedRefParser.TryExtract("   ").Should().BeNull();
    }

    [Fact]
    public void TryExtract_NoMarker_ReturnsNull()
    {
        var desc = "Просто текст обращения без всякого маркера.";
        SupportDeceasedRefParser.TryExtract(desc).Should().BeNull();
    }

    [Fact]
    public void TryExtract_MarkerWithoutGuid_ReturnsNull()
    {
        var desc = "ID карточки: не-guid";
        SupportDeceasedRefParser.TryExtract(desc).Should().BeNull();
    }

    [Fact]
    public void TryExtract_ValidMarker_ReturnsGuid()
    {
        var expected = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var desc = $"Здравствуйте. ID карточки: {expected}\nНет фото.";

        SupportDeceasedRefParser.TryExtract(desc).Should().Be(expected);
    }

    [Fact]
    public void TryExtract_MarkerUppercaseGuid_StillRecognized()
    {
        var expected = Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
        var desc = $"ID карточки: {expected.ToString().ToUpper()}";

        SupportDeceasedRefParser.TryExtract(desc).Should().Be(expected);
    }

    [Fact]
    public void TryExtract_MultipleSpacesAfterColon_StillRecognized()
    {
        var expected = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var desc = $"ID карточки:    {expected}";

        SupportDeceasedRefParser.TryExtract(desc).Should().Be(expected);
    }

    [Fact]
    public void TryExtract_MarkerWithinLongText_ReturnsFirstMatch()
    {
        var expected = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var desc = $"""
            Добрый день! У умершего:
            Имя: Иван Иванов
            ID карточки: {expected}
            Период: 01.01.1990 — 09.06.2026

            На карточке нет главного фото — прикрепляю.
            """;

        SupportDeceasedRefParser.TryExtract(desc).Should().Be(expected);
    }

    [Fact]
    public void TryExtract_TwoMarkers_ReturnsFirst()
    {
        var first = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var second = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var desc = $"ID карточки: {first}. Потом ещё ID карточки: {second}.";

        SupportDeceasedRefParser.TryExtract(desc).Should().Be(first);
    }
}
