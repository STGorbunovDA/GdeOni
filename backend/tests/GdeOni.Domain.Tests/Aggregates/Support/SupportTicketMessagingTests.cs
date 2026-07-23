using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Tests.Aggregates.Support;

/// <summary>
/// D44. Свободная переписка в обращении.
///
/// До D44 диалога не было: админ мог написать, только пометив тикет
/// решённым, а юзер — только переоткрыв его. Здесь проверяем, что
/// обычные сообщения работают и не ломают прежние правила статусов.
/// </summary>
public sealed class SupportTicketMessagingTests
{
    private static readonly DateTime Now = new(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();

    [Fact]
    public void AddUserMessage_OnOpenTicket_IsAppended()
    {
        var t = CreateOpenTicket();

        var result = t.AddUserMessage(UserId, "Оплатил, чек приложил", Now.AddMinutes(5));

        result.IsSuccess.Should().BeTrue();
        t.Messages.Should().ContainSingle();
        t.Messages.Single().Text.Should().Be("Оплатил, чек приложил");
        t.Messages.Single().AuthorKind.Should().Be(SupportTicketMessageAuthorKind.User);
    }

    /// <summary>
    /// Админский список сортирует и подсвечивает тикеты по последней
    /// реплике юзера — сообщение обязано туда попадать, иначе свежий
    /// ответ не поднимет обращение в работе.
    /// </summary>
    [Fact]
    public void AddUserMessage_UpdatesLastUserReply()
    {
        var t = CreateOpenTicket();
        var at = Now.AddMinutes(5);

        t.AddUserMessage(UserId, "Жду реквизиты", at);

        t.LastUserReply.Should().Be("Жду реквизиты");
        t.LastUserReplyAtUtc.Should().Be(at);
        t.UpdatedAtUtc.Should().Be(at);
    }

    [Fact]
    public void AddUserMessage_OnInProgressTicket_IsAllowed()
    {
        var t = CreateOpenTicket();
        t.ChangeStatus(SupportTicketStatus.InProgress, AdminId, null, Now.AddMinutes(1));

        var result = t.AddUserMessage(UserId, "Дополню: платил с карты Тинькофф", Now.AddMinutes(2));

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// На Resolved у юзера есть явный выбор «принять» / «переоткрыть».
    /// Подменять его обычным сообщением нельзя — иначе ReopenedCount
    /// перестанет отражать реальность.
    /// </summary>
    [Fact]
    public void AddUserMessage_OnResolvedTicket_IsRejected()
    {
        var t = CreateOpenTicket();
        t.ChangeStatus(SupportTicketStatus.Resolved, AdminId, "Доступ выдан", Now.AddMinutes(1));

        var result = t.AddUserMessage(UserId, "Спасибо!", Now.AddMinutes(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.message.status.invalid");
    }

    /// <summary>Closed — терминальный статус для юзера (D40).</summary>
    [Fact]
    public void AddUserMessage_OnClosedTicket_IsRejected()
    {
        var t = CreateOpenTicket();
        t.ForceClose(AdminId, "Дубль обращения", Now.AddMinutes(1));

        var result = t.AddUserMessage(UserId, "Верните!", Now.AddMinutes(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.message.status.invalid");
    }

    /// <summary>
    /// Обращения несут переписку об оплате и персональные данные —
    /// в чужой тикет писать нельзя даже при ошибке в слое выше.
    /// </summary>
    [Fact]
    public void AddUserMessage_FromAnotherUser_IsRejected()
    {
        var t = CreateOpenTicket();

        var result = t.AddUserMessage(Guid.NewGuid(), "Чужое обращение", Now.AddMinutes(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.modify.forbidden");
        t.Messages.Should().BeEmpty();
    }

    [Fact]
    public void AddUserMessage_EmptyText_IsRejected()
    {
        var t = CreateOpenTicket();

        var result = t.AddUserMessage(UserId, "   ", Now.AddMinutes(1));

        result.IsFailure.Should().BeTrue();
        t.Messages.Should().BeEmpty();
    }

    [Fact]
    public void AddAdminMessage_OnOpenTicket_IsAppendedWithoutStatusChange()
    {
        var t = CreateOpenTicket();

        var result = t.AddAdminMessage(AdminId, "Переведите на карту 1234", Now.AddMinutes(5));

        result.IsSuccess.Should().BeTrue();
        t.Messages.Single().AuthorKind.Should().Be(SupportTicketMessageAuthorKind.Admin);
        // Ключевое: ответ больше не требует врать статусом.
        t.Status.Should().Be(SupportTicketStatus.Open);
        t.ResolutionNote.Should().BeNull();
    }

    /// <summary>
    /// Админу статус не ограничиваем: он вправе дописать в обращение
    /// на любой стадии, в том числе после закрытия.
    /// </summary>
    [Fact]
    public void AddAdminMessage_OnClosedTicket_IsAllowed()
    {
        var t = CreateOpenTicket();
        t.ForceClose(AdminId, "Закрыто", Now.AddMinutes(1));

        var result = t.AddAdminMessage(AdminId, "Дополнение: реквизиты сменились", Now.AddMinutes(2));

        result.IsSuccess.Should().BeTrue();
        t.Status.Should().Be(SupportTicketStatus.Closed);
    }

    [Fact]
    public void AddAdminMessage_EmptyAdminId_IsRejected()
    {
        var t = CreateOpenTicket();

        var result = t.AddAdminMessage(Guid.Empty, "Текст", Now.AddMinutes(1));

        result.IsFailure.Should().BeTrue();
        t.Messages.Should().BeEmpty();
    }

    /// <summary>
    /// Полный сценарий оплаты переводом (D44): обращение → реквизиты
    /// от админа → подтверждение юзера → решение. Порядок сообщений
    /// в переписке должен сохраняться.
    /// </summary>
    [Fact]
    public void PaymentByTransfer_FullDialog_KeepsChronology()
    {
        var t = CreateOpenTicket();

        t.AddAdminMessage(AdminId, "Переведите 99 ₽ на карту 1234", Now.AddMinutes(10));
        t.AddUserMessage(UserId, "Перевёл, чек приложил", Now.AddMinutes(20));
        t.ChangeStatus(SupportTicketStatus.Resolved, AdminId, "Доступ выдан на 30 дней", Now.AddMinutes(30));

        t.Messages.Should().HaveCount(3);
        t.Messages.Select(m => m.AuthorKind).Should().ContainInOrder(
            SupportTicketMessageAuthorKind.Admin,
            SupportTicketMessageAuthorKind.User,
            SupportTicketMessageAuthorKind.Admin);
    }

    private static SupportTicket CreateOpenTicket() =>
        SupportTicket.CreateManual(
            UserId,
            SupportTicketKind.Payment,
            "Хочу оплатить подписку",
            "Закончился пробный период, как оплатить?",
            Now).Value;
}
