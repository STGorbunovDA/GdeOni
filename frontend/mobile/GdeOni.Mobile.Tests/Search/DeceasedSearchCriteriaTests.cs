using FluentAssertions;
using GdeOni.Mobile.Shared.Search;
using Xunit;

namespace GdeOni.Mobile.Tests.Search;

public class DeceasedSearchCriteriaTests
{
    // ───────────────────── IsTextFieldLongEnough ─────────────────────

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData(" ", false)]
    [InlineData("a", false)]          // 1 символ — слишком мало
    [InlineData("  a  ", false)]      // Trim даёт 1 символ
    [InlineData("ab", true)]           // ровно min
    [InlineData(" ab ", true)]         // Trim даёт 2 символа
    [InlineData("Иван", true)]
    public void IsTextFieldLongEnough_requires_at_least_two_chars_after_trim(
        string? input, bool expected)
    {
        DeceasedSearchCriteria.IsTextFieldLongEnough(input).Should().Be(expected);
    }

    // ───────────────────── CanSearch: пустые ─────────────────────

    [Fact]
    public void CanSearch_returns_false_when_nothing_is_filled()
    {
        DeceasedSearchCriteria.CanSearch(
            query: "",
            firstName: "",
            lastName: "",
            middleName: "",
            useBirthDateFilter: false,
            useDeathDateFilter: false).Should().BeFalse();
    }

    [Fact]
    public void CanSearch_returns_false_when_all_text_fields_are_single_char()
    {
        DeceasedSearchCriteria.CanSearch(
            query: "a",
            firstName: "b",
            lastName: "c",
            middleName: "d",
            useBirthDateFilter: false,
            useDeathDateFilter: false).Should().BeFalse();
    }

    // ───────────────────── CanSearch: один из критериев ─────────────────────

    [Fact]
    public void Just_firstName_is_enough()
    {
        DeceasedSearchCriteria.CanSearch(
            query: "", firstName: "Ив", lastName: "", middleName: "",
            useBirthDateFilter: false, useDeathDateFilter: false).Should().BeTrue();
    }

    [Fact]
    public void Just_lastName_is_enough()
    {
        DeceasedSearchCriteria.CanSearch(
            query: "", firstName: "", lastName: "Иванов", middleName: "",
            useBirthDateFilter: false, useDeathDateFilter: false).Should().BeTrue();
    }

    [Fact]
    public void Just_middleName_is_enough()
    {
        DeceasedSearchCriteria.CanSearch(
            query: "", firstName: "", lastName: "", middleName: "Сергеевич",
            useBirthDateFilter: false, useDeathDateFilter: false).Should().BeTrue();
    }

    [Fact]
    public void Just_legacy_query_field_is_enough()
    {
        DeceasedSearchCriteria.CanSearch(
            query: "Иванов", firstName: "", lastName: "", middleName: "",
            useBirthDateFilter: false, useDeathDateFilter: false).Should().BeTrue();
    }

    [Fact]
    public void Just_birth_date_filter_is_enough()
    {
        // E17.4.1 — поиск по одной только дате валиден.
        DeceasedSearchCriteria.CanSearch(
            query: "", firstName: "", lastName: "", middleName: "",
            useBirthDateFilter: true, useDeathDateFilter: false).Should().BeTrue();
    }

    [Fact]
    public void Just_death_date_filter_is_enough()
    {
        DeceasedSearchCriteria.CanSearch(
            query: "", firstName: "", lastName: "", middleName: "",
            useBirthDateFilter: false, useDeathDateFilter: true).Should().BeTrue();
    }

    // ───────────────────── CanSearch: комбинации ─────────────────────

    [Fact]
    public void Combined_fields_remain_valid()
    {
        DeceasedSearchCriteria.CanSearch(
            query: "", firstName: "Иван", lastName: "Иванов", middleName: "",
            useBirthDateFilter: true, useDeathDateFilter: true).Should().BeTrue();
    }
}
