using FluentAssertions;
using GdeOni.Mobile.Shared.Auth;
using Xunit;

namespace GdeOni.Mobile.Tests.Auth;

public class PasswordRulesTests
{
    // ───────────────────── IsTooShort ─────────────────────

    [Fact]
    public void IsTooShort_empty_is_not_short_so_we_dont_flag_until_user_started_typing()
    {
        PasswordRules.IsTooShort("").Should().BeFalse();
        PasswordRules.IsTooShort(null).Should().BeFalse();
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("1234567", true)]       // 7 символов — меньше Min=8
    [InlineData("12345678", false)]     // 8 символов — ровно Min
    [InlineData("123456789", false)]
    public void IsTooShort_returns_true_only_when_below_minimum(string input, bool expected)
    {
        PasswordRules.IsTooShort(input).Should().Be(expected);
    }

    // ───────────────────── IsTooLong ─────────────────────

    [Fact]
    public void IsTooLong_at_max_is_ok_above_is_too_long()
    {
        var atMax = new string('x', PasswordPolicy.MaxPasswordLength);
        var oneOver = new string('x', PasswordPolicy.MaxPasswordLength + 1);

        PasswordRules.IsTooLong(atMax).Should().BeFalse();
        PasswordRules.IsTooLong(oneOver).Should().BeTrue();
    }

    [Fact]
    public void IsTooLong_null_or_empty_is_not_too_long()
    {
        PasswordRules.IsTooLong(null).Should().BeFalse();
        PasswordRules.IsTooLong("").Should().BeFalse();
    }

    // ───────────────────── PasswordsMatch ─────────────────────

    [Theory]
    [InlineData("hunter22", "hunter22", true)]
    [InlineData("hunter22", "hunter23", false)]
    [InlineData("", "", false)]                     // оба пустые — не совпадение, иначе CanSubmit=true для пустых
    [InlineData("hunter22", "", false)]
    [InlineData("", "hunter22", false)]
    public void PasswordsMatch_validates_equality_with_non_empty_new(
        string newPwd, string confirm, bool expected)
    {
        PasswordRules.PasswordsMatch(newPwd, confirm).Should().Be(expected);
    }

    [Fact]
    public void PasswordsMatch_null_treated_as_empty()
    {
        PasswordRules.PasswordsMatch(null, null).Should().BeFalse();
        PasswordRules.PasswordsMatch("hunter22", null).Should().BeFalse();
    }

    // ───────────────────── CanSubmit ─────────────────────

    [Fact]
    public void CanSubmit_happy_path()
    {
        PasswordRules.CanSubmit("old-pass", "new-pass-1", "new-pass-1").Should().BeTrue();
    }

    [Theory]
    [InlineData("", "new-pass-1", "new-pass-1", false)]      // current пуст
    [InlineData("old", "short7", "short7", false)]            // newPwd < 8
    [InlineData("old", "new-pass-1", "new-pass-2", false)]    // confirm не совпадает
    [InlineData("old", "new-pass-1", "", false)]              // confirm пуст
    public void CanSubmit_rejects_when_any_rule_fails(
        string current, string newPwd, string confirm, bool expected)
    {
        PasswordRules.CanSubmit(current, newPwd, confirm).Should().Be(expected);
    }

    [Fact]
    public void CanSubmit_rejects_when_newPwd_exceeds_max()
    {
        var over = new string('x', PasswordPolicy.MaxPasswordLength + 1);
        PasswordRules.CanSubmit("old", over, over).Should().BeFalse();
    }
}
