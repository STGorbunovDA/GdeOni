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

    // ───────── D25.1. AcceptResolution / Reopen ─────────

    [Fact]
    public void AcceptResolution_BeforeResolved_ReturnsAcceptOnlyAfterResolved()
    {
        var userId = Guid.NewGuid();
        var t = SupportTicket.CreateManual(
            userId, SupportTicketKind.Other, "T", "D", Now).Value;

        var result = t.AcceptResolution(userId, Now.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.accept.only_after_resolved");
    }

    [Fact]
    public void AcceptResolution_NotAuthor_ReturnsModifyForbidden()
    {
        var (t, _) = NewResolvedWithAuthor();

        var result = t.AcceptResolution(Guid.NewGuid(), Now.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.modify.forbidden");
    }

    [Fact]
    public void AcceptResolution_HappyPath_SetsFlagAndTimestamp()
    {
        var (t, author) = NewResolvedWithAuthor();

        var result = t.AcceptResolution(author, Now.AddHours(2));

        result.IsSuccess.Should().BeTrue();
        t.AcceptedByUser.Should().BeTrue();
        t.AcceptedByUserAtUtc.Should().Be(Now.AddHours(2));
        t.UpdatedAtUtc.Should().Be(Now.AddHours(2));
        t.Status.Should().Be(SupportTicketStatus.Resolved);
    }

    [Fact]
    public void AcceptResolution_Twice_ReturnsAlreadyAccepted()
    {
        var (t, author) = NewResolvedWithAuthor();
        t.AcceptResolution(author, Now.AddHours(1));

        var second = t.AcceptResolution(author, Now.AddHours(3));

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("support_ticket.already.accepted");
    }

    [Fact]
    public void Reopen_BeforeResolved_ReturnsReopenOnlyAfterResolved()
    {
        var userId = Guid.NewGuid();
        var t = SupportTicket.CreateManual(
            userId, SupportTicketKind.Other, "T", "D", Now).Value;

        var result = t.Reopen(userId, "не подходит", Now.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.reopen.only_after_resolved");
    }

    [Fact]
    public void Reopen_NotAuthor_ReturnsModifyForbidden()
    {
        var (t, _) = NewResolvedWithAuthor();

        var result = t.Reopen(Guid.NewGuid(), "stranger", Now.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.modify.forbidden");
    }

    [Fact]
    public void Reopen_HappyPath_StatusBackToOpen_CounterIncremented_ResolutionKept()
    {
        var (t, author) = NewResolvedWithAuthor();
        var originalNote = t.ResolutionNote;

        var result = t.Reopen(author, "не помогло — деньги не вернулись", Now.AddHours(3));

        result.IsSuccess.Should().BeTrue();
        t.Status.Should().Be(SupportTicketStatus.Open);
        t.ReopenedCount.Should().Be(1);
        t.LastUserReply.Should().Be("не помогло — деньги не вернулись");
        t.LastUserReplyAtUtc.Should().Be(Now.AddHours(3));
        t.AcceptedByUser.Should().BeFalse();
        // История админа сохраняется — note и resolved_at остаются.
        t.ResolutionNote.Should().Be(originalNote);
        t.ResolvedByUserId.Should().NotBeNull();
        t.ResolvedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Reopen_AfterAccept_ReturnsAlreadyAccepted()
    {
        var (t, author) = NewResolvedWithAuthor();
        t.AcceptResolution(author, Now.AddHours(1));

        var result = t.Reopen(author, "передумал", Now.AddHours(3));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.already.accepted");
    }

    [Fact]
    public void Reopen_TooLongReply_ReturnsUserReplyTooLong()
    {
        var (t, author) = NewResolvedWithAuthor();
        var longReply = new string('x', SupportTicket.MaxUserReplyLength + 1);

        var result = t.Reopen(author, longReply, Now.AddHours(3));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.user_reply.too_long");
    }

    [Fact]
    public void Reopen_NullReply_AllowedKeepsLastUserReplyNull()
    {
        var (t, author) = NewResolvedWithAuthor();

        var result = t.Reopen(author, null, Now.AddHours(3));

        result.IsSuccess.Should().BeTrue();
        t.LastUserReply.Should().BeNull();
        t.LastUserReplyAtUtc.Should().BeNull();
        t.ReopenedCount.Should().Be(1);
    }

    [Fact]
    public void Reopen_Twice_IncrementsCounter()
    {
        var (t, author) = NewResolvedWithAuthor();
        t.Reopen(author, "first", Now.AddHours(1));
        // Админ снова Resolved
        t.ChangeStatus(SupportTicketStatus.Resolved, Guid.NewGuid(), "повторный ответ", Now.AddHours(2));

        var second = t.Reopen(author, "second", Now.AddHours(3));

        second.IsSuccess.Should().BeTrue();
        t.ReopenedCount.Should().Be(2);
        t.LastUserReply.Should().Be("second");
    }

    private static (SupportTicket Ticket, Guid AuthorId) NewResolvedWithAuthor()
    {
        var author = Guid.NewGuid();
        var t = SupportTicket.CreateManual(
            author, SupportTicketKind.Payment, "Не пришла оплата", "1000р не вижу", Now).Value;
        t.ChangeStatus(SupportTicketStatus.Resolved, Guid.NewGuid(), "Зачислили — проверьте.", Now.AddMinutes(30));
        return (t, author);
    }

    private static SupportTicket NewOpen() =>
        SupportTicket.CreateManual(
            Guid.NewGuid(),
            SupportTicketKind.Bug,
            "Тест",
            "Тест",
            Now).Value;
}
