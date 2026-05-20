using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Tests.UserAggregate;

/// <summary>
/// D22. Тесты на выдачу и отзыв бесплатного доступа админом:
/// User.GrantComplimentaryAccess + RevokeComplimentaryAccess +
/// HasComplimentaryAccess + no-op guard.
/// </summary>
public sealed class ComplimentaryAccessTests
{
    private const string SampleEmail = "user@example.com";
    private const string SampleHash = "hash$with$enough$chars";
    private static readonly Guid AdminId = Guid.NewGuid();

    [Fact]
    public void NewUser_HasNoComplimentaryAccess()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;

        user.ComplimentaryAccessGrantedAtUtc.Should().BeNull();
        user.ComplimentaryAccessUntilUtc.Should().BeNull();
        user.ComplimentaryAccessGrantedByAdminId.Should().BeNull();
        user.ComplimentaryAccessNote.Should().BeNull();
        user.HasComplimentaryAccess(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void Grant_Unlimited_SetsFourFieldsAndKeepsSecurityStamp()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        var stampBefore = user.SecurityStamp;
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);

        var result = user.GrantComplimentaryAccess(AdminId, untilUtc: null, note: "friend", nowUtc: now);

        result.IsSuccess.Should().BeTrue();
        user.ComplimentaryAccessGrantedAtUtc.Should().Be(now);
        user.ComplimentaryAccessUntilUtc.Should().BeNull();
        user.ComplimentaryAccessGrantedByAdminId.Should().Be(AdminId);
        user.ComplimentaryAccessNote.Should().Be("friend");
        user.SecurityStamp.Should().Be(stampBefore,
            "admin-action не должна форсить юзера разлогиниться");
    }

    [Fact]
    public void Grant_UntilFuture_StoresExpiryDate()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        var until = now.AddDays(90);

        var result = user.GrantComplimentaryAccess(AdminId, untilUtc: until, note: null, nowUtc: now);

        result.IsSuccess.Should().BeTrue();
        user.ComplimentaryAccessUntilUtc.Should().Be(until);
        user.ComplimentaryAccessNote.Should().BeNull();
    }

    [Fact]
    public void Grant_EmptyAdminId_ReturnsValidationError()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;

        var result = user.GrantComplimentaryAccess(Guid.Empty, null, null, DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("complimentary.admin_id.required");
    }

    [Fact]
    public void Grant_UntilInPast_ReturnsValidationError()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);

        var result = user.GrantComplimentaryAccess(AdminId, untilUtc: now.AddDays(-1), null, now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("complimentary.until.in_past");
    }

    [Fact]
    public void Grant_NoteTooLong_ReturnsValidationError()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        var hugeNote = new string('x', User.MaxComplimentaryNoteLength + 1);

        var result = user.GrantComplimentaryAccess(AdminId, null, hugeNote, DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("complimentary.note.too_long");
    }

    [Fact]
    public void Grant_SameValuesTwice_IsNoOp_DoesNotChangeGrantedAt()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        var firstGrantAt = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        user.GrantComplimentaryAccess(AdminId, null, "promo", firstGrantAt);

        var secondCall = firstGrantAt.AddDays(7);
        var result = user.GrantComplimentaryAccess(AdminId, null, "promo", secondCall);

        result.IsSuccess.Should().BeTrue();
        user.ComplimentaryAccessGrantedAtUtc.Should().Be(firstGrantAt,
            "no-op не должен двигать GrantedAt при идентичных параметрах");
    }

    [Fact]
    public void Grant_ChangedUntil_UpdatesGrantedAt()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        var firstAt = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        user.GrantComplimentaryAccess(AdminId, untilUtc: null, note: null, firstAt);

        var secondAt = firstAt.AddDays(7);
        var newUntil = secondAt.AddDays(30);
        var result = user.GrantComplimentaryAccess(AdminId, untilUtc: newUntil, note: null, secondAt);

        result.IsSuccess.Should().BeTrue();
        user.ComplimentaryAccessGrantedAtUtc.Should().Be(secondAt);
        user.ComplimentaryAccessUntilUtc.Should().Be(newUntil);
    }

    [Fact]
    public void Revoke_AfterGrant_ClearsAllFields()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        user.GrantComplimentaryAccess(AdminId, null, "promo", DateTime.UtcNow);

        var result = user.RevokeComplimentaryAccess();

        result.IsSuccess.Should().BeTrue();
        user.ComplimentaryAccessGrantedAtUtc.Should().BeNull();
        user.ComplimentaryAccessUntilUtc.Should().BeNull();
        user.ComplimentaryAccessGrantedByAdminId.Should().BeNull();
        user.ComplimentaryAccessNote.Should().BeNull();
    }

    [Fact]
    public void Revoke_NeverGranted_IsNoOp()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;

        var result = user.RevokeComplimentaryAccess();

        result.IsSuccess.Should().BeTrue();
        user.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void HasComplimentaryAccess_NotGranted_ReturnsFalse()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;

        user.HasComplimentaryAccess(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void HasComplimentaryAccess_GrantedUnlimited_ReturnsTrue()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        user.GrantComplimentaryAccess(AdminId, untilUtc: null, null, DateTime.UtcNow);

        user.HasComplimentaryAccess(DateTime.UtcNow.AddYears(10)).Should().BeTrue();
    }

    [Fact]
    public void HasComplimentaryAccess_UntilInFuture_ReturnsTrue()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        user.GrantComplimentaryAccess(AdminId, now.AddDays(30), null, now);

        user.HasComplimentaryAccess(now.AddDays(15)).Should().BeTrue();
    }

    [Fact]
    public void HasComplimentaryAccess_UntilPassed_ReturnsFalse()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        user.GrantComplimentaryAccess(AdminId, now.AddDays(7), null, now);

        user.HasComplimentaryAccess(now.AddDays(10)).Should().BeFalse();
    }

    [Fact]
    public void Grant_WhitespaceNote_NormalizesToNull()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;

        user.GrantComplimentaryAccess(AdminId, null, "   ", DateTime.UtcNow);

        user.ComplimentaryAccessNote.Should().BeNull();
    }

    [Fact]
    public void Grant_NoteWithSurroundingWhitespace_IsTrimmed()
    {
        var user = User.Register(SampleEmail, SampleHash).Value;

        user.GrantComplimentaryAccess(AdminId, null, "  hello  ", DateTime.UtcNow);

        user.ComplimentaryAccessNote.Should().Be("hello");
    }
}
