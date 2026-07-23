using FluentAssertions;
using GdeOni.Mobile.Shared.EditsHistory;
using Xunit;

namespace GdeOni.Mobile.Tests.EditsHistory;

public class EditsHistoryParserTests
{
    [Fact]
    public void ExtractPreviousAuthorEmail_NullJson_ReturnsNull()
    {
        EditsHistoryParser.ExtractPreviousAuthorEmail(null).Should().BeNull();
    }

    [Fact]
    public void ExtractPreviousAuthorEmail_EmptyJson_ReturnsNull()
    {
        EditsHistoryParser.ExtractPreviousAuthorEmail("").Should().BeNull();
        EditsHistoryParser.ExtractPreviousAuthorEmail("   ").Should().BeNull();
    }

    [Fact]
    public void ExtractPreviousAuthorEmail_GarbageJson_ReturnsNull()
    {
        EditsHistoryParser.ExtractPreviousAuthorEmail("not json at all").Should().BeNull();
    }

    [Fact]
    public void ExtractPreviousAuthorEmail_NoPreviousAuthorKey_ReturnsNull()
    {
        var json = """{ "FirstName": { "old": "Иван", "new": "Пётр" } }""";
        EditsHistoryParser.ExtractPreviousAuthorEmail(json).Should().BeNull();
    }

    [Fact]
    public void ExtractPreviousAuthorEmail_HasPreviousAuthor_ReturnsOldValue()
    {
        var json = """{ "PreviousAuthor": { "old": "user@example.com", "new": null } }""";
        EditsHistoryParser.ExtractPreviousAuthorEmail(json).Should().Be("user@example.com");
    }

    [Fact]
    public void ExtractPreviousAuthorEmail_PreviousAuthorWithNullOld_ReturnsNull()
    {
        var json = """{ "PreviousAuthor": { "old": null, "new": null } }""";
        EditsHistoryParser.ExtractPreviousAuthorEmail(json).Should().BeNull();
    }

    [Fact]
    public void ParseChanges_NullJson_ReturnsEmpty()
    {
        EditsHistoryParser.ParseChanges(null).Should().BeEmpty();
    }

    [Fact]
    public void ParseChanges_EmptyJson_ReturnsEmpty()
    {
        EditsHistoryParser.ParseChanges("").Should().BeEmpty();
    }

    [Fact]
    public void ParseChanges_SingleField_ReturnsOneRow()
    {
        var json = """{ "FirstName": { "old": "Иван", "new": "Пётр" } }""";
        var rows = EditsHistoryParser.ParseChanges(json);

        rows.Should().HaveCount(1);
        rows[0].FieldLabel.Should().Be("Имя");
        rows[0].OldValue.Should().Be("Иван");
        rows[0].NewValue.Should().Be("Пётр");
    }

    [Fact]
    public void ParseChanges_NullValues_ShownAsDash()
    {
        var json = """{ "MiddleName": { "old": null, "new": null } }""";
        var rows = EditsHistoryParser.ParseChanges(json);

        rows.Should().HaveCount(1);
        rows[0].OldValue.Should().Be("—");
        rows[0].NewValue.Should().Be("—");
    }

    [Fact]
    public void ParseChanges_UnknownField_KeepsRawKey()
    {
        var json = """{ "WeirdNewField": { "old": "a", "new": "b" } }""";
        var rows = EditsHistoryParser.ParseChanges(json);

        rows[0].FieldLabel.Should().Be("WeirdNewField");
    }

    [Fact]
    public void ParseChanges_MultipleFields_ReturnsAll()
    {
        var json = """
            {
              "FirstName": { "old": "Иван", "new": "Пётр" },
              "City": { "old": "Москва", "new": "Тула" }
            }
            """;
        var rows = EditsHistoryParser.ParseChanges(json);

        rows.Should().HaveCount(2);
        rows.Select(r => r.FieldLabel).Should().Contain(new[] { "Имя", "Город" });
    }

    [Fact]
    public void ParseChanges_BrokenJson_ReturnsFallback()
    {
        var json = "{ broken";
        var rows = EditsHistoryParser.ParseChanges(json);

        rows.Should().HaveCount(1);
        rows[0].FieldLabel.Should().Be("(diff)");
        rows[0].NewValue.Should().Be(json);
    }

    [Theory]
    [InlineData("FirstName", "Имя")]
    [InlineData("LastName", "Фамилия")]
    [InlineData("MiddleName", "Отчество")]
    [InlineData("BirthDate", "Дата рождения")]
    [InlineData("DeathDate", "Дата смерти")]
    [InlineData("ShortDescription", "Краткое описание")]
    [InlineData("Biography", "Биография")]
    [InlineData("Epitaph", "Эпитафия")]
    [InlineData("Religion", "Религия")]
    [InlineData("Source", "Источник")]
    [InlineData("IsMilitaryService", "Военная служба")]
    [InlineData("AdditionalInfo", "Доп. информация")]
    [InlineData("Latitude", "Широта")]
    [InlineData("Longitude", "Долгота")]
    [InlineData("AccuracyMeters", "Точность, м")]
    [InlineData("Country", "Страна")]
    [InlineData("City", "Город")]
    [InlineData("CemeteryName", "Кладбище")]
    [InlineData("PlotNumber", "Участок")]
    [InlineData("GraveNumber", "Номер могилы")]
    [InlineData("PreviousAuthor", "Удалённый автор (email)")]
    public void FieldDisplay_KnownFields_ReturnsRussianLabel(string field, string expected)
    {
        EditsHistoryParser.FieldDisplay(field).Should().Be(expected);
    }

    [Fact]
    public void FieldDisplay_UnknownField_ReturnsAsIs()
    {
        EditsHistoryParser.FieldDisplay("SomeFutureField").Should().Be("SomeFutureField");
    }
}
