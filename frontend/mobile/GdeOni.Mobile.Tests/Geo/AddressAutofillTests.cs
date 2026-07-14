using FluentAssertions;
using GdeOni.Mobile.Shared.Geo;
using Xunit;

namespace GdeOni.Mobile.Tests.Geo;

/// <summary>
/// D41. Те же случаи, что в web-тесте addressAutofill.test.ts — правило
/// автоподстановки обязано совпадать на обоих клиентах.
/// </summary>
public class AddressAutofillTests
{
    [Fact]
    public void Merge_EmptyField_TakesIncoming()
    {
        AddressAutofill.Merge("", "", "Москва").Should().Be("Москва");
    }

    [Fact]
    public void Merge_ValueWeFilledEarlier_IsUpdated()
    {
        // Юзер сдвинул точку с Твери на Москву — город должен переехать.
        AddressAutofill.Merge("Тверь", "Тверь", "Москва").Should().Be("Москва");
    }

    [Fact]
    public void Merge_ManuallyEditedField_IsKept()
    {
        // Определилось «Химки», но человек вписал «Москва» — его слово главнее.
        AddressAutofill.Merge("Москва", "Мытищи", "Химки").Should().Be("Москва");
    }

    [Fact]
    public void Merge_NoIncoming_KeepsCurrent()
    {
        AddressAutofill.Merge("Москва", "Москва", null).Should().Be("Москва");
        AddressAutofill.Merge("", "", null).Should().Be("");
    }

    [Fact]
    public void Merge_WhitespaceIsNotManualInput()
    {
        AddressAutofill.Merge("   ", "", "Москва").Should().Be("Москва");
        AddressAutofill.Merge(" Тверь ", "Тверь", "Москва").Should().Be("Москва");
    }
}
