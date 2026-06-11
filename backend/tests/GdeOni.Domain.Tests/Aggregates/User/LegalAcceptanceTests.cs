using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

// Namespace отличается от корневого, чтобы не конфликтовать с
// типом GdeOni.Domain.Aggregates.User.User (та же история, что
// и в UserTests.cs).
namespace GdeOni.Domain.Tests.UserAggregate;

/// <summary>
/// D19. Тесты на принятие Privacy Policy и Terms of Use:
/// User.AcceptLegal + HasOutdatedLegalAcceptance + no-op guard.
/// </summary>
public sealed class LegalAcceptanceTests
{
    private const string SampleEmail = "user@example.com";
    private const string SampleHash = "hash$with$enough$chars";

    [Fact]
    public void NewUser_HasNoLegalAcceptance()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;

        user.PrivacyPolicyAcceptedAtUtc.Should().BeNull();
        user.TermsAcceptedAtUtc.Should().BeNull();
        user.PrivacyPolicyVersion.Should().Be(0);
        user.TermsVersion.Should().Be(0);
    }

    [Fact]
    public void AcceptLegal_SetsTimestampsAndVersions()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        var now = new DateTime(2026, 5, 19, 12, 0, 0, DateTimeKind.Utc);

        var result = user.AcceptLegal(privacyPolicyVersion: 2, termsVersion: 3, nowUtc: now);

        result.IsSuccess.Should().BeTrue();
        user.PrivacyPolicyAcceptedAtUtc.Should().Be(now);
        user.TermsAcceptedAtUtc.Should().Be(now);
        user.PrivacyPolicyVersion.Should().Be(2);
        user.TermsVersion.Should().Be(3);
    }

    [Fact]
    public void AcceptLegal_InvalidPrivacyVersion_ReturnsValidationError()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;

        var result = user.AcceptLegal(privacyPolicyVersion: 0, termsVersion: 1, nowUtc: DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("legal.privacy_policy.version.invalid");
    }

    [Fact]
    public void AcceptLegal_InvalidTermsVersion_ReturnsValidationError()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;

        var result = user.AcceptLegal(privacyPolicyVersion: 1, termsVersion: 0, nowUtc: DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("legal.terms.version.invalid");
    }

    [Fact]
    public void AcceptLegal_SameVersions_NoOp_DoesNotChangeTimestamp()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        var firstTimestamp = new DateTime(2026, 5, 19, 12, 0, 0, DateTimeKind.Utc);
        user.AcceptLegal(1, 1, firstTimestamp);

        var secondTimestamp = firstTimestamp.AddDays(7);
        var result = user.AcceptLegal(1, 1, secondTimestamp);

        result.IsSuccess.Should().BeTrue();
        // No-op: timestamps остаются первыми.
        user.PrivacyPolicyAcceptedAtUtc.Should().Be(firstTimestamp);
        user.TermsAcceptedAtUtc.Should().Be(firstTimestamp);
    }

    [Fact]
    public void AcceptLegal_BumpedVersion_UpdatesTimestamp()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        var firstTimestamp = new DateTime(2026, 5, 19, 12, 0, 0, DateTimeKind.Utc);
        user.AcceptLegal(1, 1, firstTimestamp);

        var secondTimestamp = firstTimestamp.AddDays(7);
        var result = user.AcceptLegal(2, 1, secondTimestamp);

        result.IsSuccess.Should().BeTrue();
        user.PrivacyPolicyAcceptedAtUtc.Should().Be(secondTimestamp);
        user.TermsAcceptedAtUtc.Should().Be(secondTimestamp);
        user.PrivacyPolicyVersion.Should().Be(2);
        user.TermsVersion.Should().Be(1);
    }

    [Fact]
    public void HasOutdatedLegalAcceptance_NeverAccepted_ReturnsTrue()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;

        user.HasOutdatedLegalAcceptance(1, 1).Should().BeTrue();
    }

    [Fact]
    public void HasOutdatedLegalAcceptance_AcceptedCurrentVersions_ReturnsFalse()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        user.AcceptLegal(2, 3, DateTime.UtcNow);

        user.HasOutdatedLegalAcceptance(2, 3).Should().BeFalse();
    }

    [Fact]
    public void HasOutdatedLegalAcceptance_PrivacyBumped_ReturnsTrue()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        user.AcceptLegal(1, 1, DateTime.UtcNow);

        user.HasOutdatedLegalAcceptance(2, 1).Should().BeTrue();
    }

    [Fact]
    public void HasOutdatedLegalAcceptance_TermsBumped_ReturnsTrue()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        user.AcceptLegal(1, 1, DateTime.UtcNow);

        user.HasOutdatedLegalAcceptance(1, 2).Should().BeTrue();
    }
}
