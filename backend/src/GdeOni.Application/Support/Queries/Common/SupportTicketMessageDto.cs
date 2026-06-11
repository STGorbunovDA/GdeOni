using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.Common;

/// <summary>
/// D25.2. Сообщение в переписке тикета. Отдаётся клиенту в карточке
/// в хронологическом порядке.
/// </summary>
public sealed class SupportTicketMessageDto
{
    public Guid Id { get; set; }
    public SupportTicketMessageAuthorKind AuthorKind { get; set; }
    public Guid? AuthorUserId { get; set; }
    public string Text { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
