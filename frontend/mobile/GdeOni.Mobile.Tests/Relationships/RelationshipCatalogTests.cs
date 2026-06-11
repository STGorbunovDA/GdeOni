using FluentAssertions;
using GdeOni.Mobile.Shared.Relationships;
using Xunit;

namespace GdeOni.Mobile.Tests.Relationships;

public class RelationshipCatalogTests
{
    [Fact]
    public void All_contains_all_nine_relationship_types()
    {
        // Если добавили новый relationship type — backend enum поменялся,
        // нужно обновить и Shared.RelationshipCatalog.All, и backend, и
        // mobile/web формы. Тест ловит расхождение списка.
        RelationshipCatalog.All.Should().HaveCount(9);
        RelationshipCatalog.All.Select(o => o.Value).Should().BeEquivalentTo(new[]
        {
            "Parent", "Grandparent", "Child", "Spouse", "Sibling",
            "Relative", "Friend", "Acquaintance", "Other"
        });
    }

    [Theory]
    [InlineData("Parent", "Родитель")]
    [InlineData("Grandparent", "Дедушка / бабушка")]
    [InlineData("Friend", "Друг")]
    [InlineData("Other", "Другое")]
    public void Display_returns_russian_label_for_known_value(string value, string expected)
    {
        RelationshipCatalog.Display(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("parent", "Родитель")]    // lower-case
    [InlineData("PARENT", "Родитель")]    // upper-case
    [InlineData("Parent", "Родитель")]    // canonical
    public void Display_is_case_insensitive(string value, string expected)
    {
        RelationshipCatalog.Display(value).Should().Be(expected);
    }

    [Fact]
    public void Display_returns_em_dash_for_null_or_empty()
    {
        RelationshipCatalog.Display(null).Should().Be("—");
        RelationshipCatalog.Display(string.Empty).Should().Be("—");
    }

    [Fact]
    public void Display_lenient_fallback_returns_input_when_value_unknown()
    {
        // Защита от ситуации "backend добавил новый тип, mobile ещё не
        // обновлён" — отображаем неизвестное значение как есть, а не
        // null или "Error".
        RelationshipCatalog.Display("NewlyAddedRelationship").Should().Be("NewlyAddedRelationship");
    }
}
