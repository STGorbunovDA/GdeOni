using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Tests.Aggregates.Support;

/// <summary>
/// D25. Доменные инварианты SupportTicket: создание, смена статуса,
/// смена severity, идемпотентность, переход в Resolved.
/// </summary>
public sealed class SupportTicketTests
{
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateManual_HappyPath_ReturnsOpenNormalTicket()
    {
        var userId = Guid.NewGuid();

        var result = SupportTicket.CreateManual(
            userId,
            SupportTicketKind.Payment,
            "Не приходит подтверждение оплаты",
            "Оплатил час назад, статус всё ещё Pending.",
            Now);

        result.IsSuccess.Should().BeTrue();
        var t = result.Value;
        t.UserId.Should().Be(userId);
        t.Source.Should().Be(SupportTicketSource.Manual);
        t.Kind.Should().Be(SupportTicketKind.Payment);
        t.Severity.Should().Be(SupportTicketSeverity.Normal);
        t.Status.Should().Be(SupportTicketStatus.Open);
        t.CreatedAtUtc.Should().Be(Now);
        t.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void CreateManual_EmptyUserId_ReturnsError()
    {
        var result = SupportTicket.CreateManual(
            Guid.Empty,
            SupportTicketKind.Bug,
            "Title",
            "Description",
            Now);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(SupportTicketKind.Unknown)]
    public void CreateManual_UnknownKind_ReturnsKindInvalid(SupportTicketKind kind)
    {
        var result = SupportTicket.CreateManual(
            Guid.NewGuid(), kind, "T", "D", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.kind.invalid");
    }

    [Fact]
    public void CreateManual_EmptyTitle_ReturnsTitleRequired()
    {
        var result = SupportTicket.CreateManual(
            Guid.NewGuid(), SupportTicketKind.Other, "   ", "Desc", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.title.required");
    }

    [Fact]
    public void CreateManual_TitleTooLong_ReturnsTitleTooLong()
    {
        var longTitle = new string('x', SupportTicket.MaxTitleLength + 1);

        var result = SupportTicket.CreateManual(
            Guid.NewGuid(), SupportTicketKind.Other, longTitle, "Desc", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.title.too_long");
    }

    [Fact]
    public void CreateAuto_NullUser_AllowedForUnknownPayer()
    {
        var result = SupportTicket.CreateAuto(
            userId: null,
            kind: SupportTicketKind.Payment,
            severity: SupportTicketSeverity.Urgent,
            title: "Webhook payment not found",
            description: "yk-id 'xxx' не найден в БД",
            details: "{\"externalPaymentId\":\"xxx\"}",
            nowUtc: Now);

        result.IsSuccess.Should().BeTrue();
        var t = result.Value;
        t.Source.Should().Be(SupportTicketSource.Auto);
        t.Severity.Should().Be(SupportTicketSeverity.Urgent);
        t.UserId.Should().BeNull();
        t.Details.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateAuto_UnknownSeverity_ReturnsSeverityInvalid()
    {
        var result = SupportTicket.CreateAuto(
            userId: null,
            kind: SupportTicketKind.Payment,
            severity: SupportTicketSeverity.Unknown,
            title: "T",
            description: "D",
            details: null,
            nowUtc: Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.severity.invalid");
    }

    [Fact]
    public void ChangeStatus_OpenToInProgress_Updates()
    {
        var t = NewOpen();
        var admin = Guid.NewGuid();

        var result = t.ChangeStatus(
            SupportTicketStatus.InProgress, admin, null, Now.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        t.Status.Should().Be(SupportTicketStatus.InProgress);
        t.UpdatedAtUtc.Should().Be(Now.AddHours(1));
        t.ResolvedAtUtc.Should().BeNull();
        t.ResolutionNote.Should().BeNull();
    }

    [Fact]
    public void ChangeStatus_ToResolvedWithoutNote_ReturnsResolutionNoteRequired()
    {
        var t = NewOpen();

        var result = t.ChangeStatus(
            SupportTicketStatus.Resolved, Guid.NewGuid(), null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.resolution_note.required");
        t.Status.Should().Be(SupportTicketStatus.Open);
    }

    [Fact]
    public void ChangeStatus_ToResolvedWithNote_FixesAdminAndTimestamp()
    {
        var t = NewOpen();
        var admin = Guid.NewGuid();

        var result = t.ChangeStatus(
            SupportTicketStatus.Resolved, admin, "Выдан compli на 30 дней", Now.AddHours(2));

        result.IsSuccess.Should().BeTrue();
        t.Status.Should().Be(SupportTicketStatus.Resolved);
        t.ResolvedByUserId.Should().Be(admin);
        t.ResolvedAtUtc.Should().Be(Now.AddHours(2));
        t.ResolutionNote.Should().Be("Выдан compli на 30 дней");
    }

    [Fact]
    public void ChangeStatus_AlreadyResolved_ReturnsAlreadyResolved()
    {
        var t = NewOpen();
        t.ChangeStatus(SupportTicketStatus.Resolved, Guid.NewGuid(), "ok", Now);

        var second = t.ChangeStatus(
            SupportTicketStatus.InProgress, Guid.NewGuid(), null, Now.AddHours(1));

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("support_ticket.already.resolved");
    }

    [Fact]
    public void ChangeStatus_SameStatus_NoOp()
    {
        var t = NewOpen();
        var updatedBefore = t.UpdatedAtUtc;

        var result = t.ChangeStatus(
            SupportTicketStatus.Open, Guid.NewGuid(), null, Now.AddHours(5));

        result.IsSuccess.Should().BeTrue();
        t.UpdatedAtUtc.Should().Be(updatedBefore); // не дёргается
    }

    [Fact]
    public void ChangeSeverity_NormalToUrgent_Updates()
    {
        var t = NewOpen();

        var result = t.ChangeSeverity(SupportTicketSeverity.Urgent, Now.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        t.Severity.Should().Be(SupportTicketSeverity.Urgent);
        t.UpdatedAtUtc.Should().Be(Now.AddHours(1));
    }

    [Fact]
    public void ChangeSeverity_OnResolved_Forbidden()
    {
        var t = NewOpen();
        t.ChangeStatus(SupportTicketStatus.Resolved, Guid.NewGuid(), "ok", Now);

        var result = t.ChangeSeverity(SupportTicketSeverity.Urgent, Now.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.already.resolved");
    }

    [Fact]
    public void ChangeSeverity_SameValue_NoOp()
    {
        var t = NewOpen();
        var updatedBefore = t.UpdatedAtUtc;

        var result = t.ChangeSeverity(SupportTicketSeverity.Normal, Now.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        t.UpdatedAtUtc.Should().Be(updatedBefore);
    }

    private static SupportTicket NewOpen() =>
        SupportTicket.CreateManual(
            Guid.NewGuid(),
            SupportTicketKind.Bug,
            "Тест",
            "Тест",
            Now).Value;
}
