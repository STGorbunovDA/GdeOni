namespace GdeOni.Domain.Shared;

/// <summary>
/// Приоритет обращения. Ортогонален <see cref="SupportTicketStatus"/>:
/// тикет может быть InProgress+Urgent одновременно (срочный, в работе).
/// </summary>
public enum SupportTicketSeverity
{
    Unknown = 0,
    Normal = 1,
    Urgent = 2
}
