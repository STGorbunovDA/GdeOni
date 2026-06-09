using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Support;

/// <summary>
/// D25.2. Сообщение в переписке тикета поддержки. Принадлежит
/// агрегату SupportTicket — наружу выдаётся как IReadOnlyCollection,
/// мутации только через доменные методы (CreateFromUser/CreateFromAdmin).
/// Отдельная таблица support_ticket_messages, отсортирована по
/// CreatedAtUtc ASC (хронология чата).
/// </summary>
public sealed class SupportTicketMessage : Entity<Guid>
{
    public const int MaxTextLength = 4000;

    public Guid TicketId { get; private set; }
    public SupportTicketMessageAuthorKind AuthorKind { get; private set; }

    /// <summary>
    /// Кто конкретно отправил. Для User — это автор тикета (SupportTicket.UserId).
    /// Для Admin — админ-исполнитель. Nullable: если юзер удалён, FK
    /// SetNull, сообщение в истории остаётся.
    /// </summary>
    public Guid? AuthorUserId { get; private set; }

    public string Text { get; private set; }
    public DateTime CreatedAtUtc { get; }

    private SupportTicketMessage() : base(Guid.Empty)
    {
        Text = null!;
    }

    private SupportTicketMessage(
        Guid id,
        Guid ticketId,
        SupportTicketMessageAuthorKind authorKind,
        Guid? authorUserId,
        string text,
        DateTime createdAtUtc) : base(id)
    {
        TicketId = ticketId;
        AuthorKind = authorKind;
        AuthorUserId = authorUserId;
        Text = text;
        CreatedAtUtc = createdAtUtc;
    }

    internal static Result<SupportTicketMessage, Error> CreateFromUser(
        Guid ticketId,
        Guid authorUserId,
        string text,
        DateTime nowUtc)
        => Create(ticketId, SupportTicketMessageAuthorKind.User, authorUserId, text, nowUtc);

    internal static Result<SupportTicketMessage, Error> CreateFromAdmin(
        Guid ticketId,
        Guid authorUserId,
        string text,
        DateTime nowUtc)
        => Create(ticketId, SupportTicketMessageAuthorKind.Admin, authorUserId, text, nowUtc);

    private static Result<SupportTicketMessage, Error> Create(
        Guid ticketId,
        SupportTicketMessageAuthorKind kind,
        Guid? authorUserId,
        string text,
        DateTime nowUtc)
    {
        if (ticketId == Guid.Empty)
            return Errors.General.ValueIsRequired("ticketId");

        if (string.IsNullOrWhiteSpace(text))
            return Errors.Support.MessageTextRequired();

        var trimmed = text.Trim();
        if (trimmed.Length > MaxTextLength)
            return Errors.Support.MessageTextTooLong(MaxTextLength);

        return new SupportTicketMessage(
            Guid.NewGuid(),
            ticketId,
            kind,
            authorUserId == Guid.Empty ? null : authorUserId,
            trimmed,
            nowUtc);
    }
}
