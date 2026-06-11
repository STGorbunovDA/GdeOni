namespace GdeOni.Domain.Shared;

/// <summary>
/// Кто автор сообщения в переписке тикета. User — это автор обращения
/// (UserId совпадает с SupportTicket.UserId). Admin — резолютор
/// (AuthorUserId = админ-исполнитель).
/// </summary>
public enum SupportTicketMessageAuthorKind
{
    Unknown = 0,
    User = 1,
    Admin = 2
}
