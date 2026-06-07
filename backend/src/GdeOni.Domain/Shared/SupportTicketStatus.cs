namespace GdeOni.Domain.Shared;

/// <summary>
/// Состояние обработки обращения. Open — никто ещё не взял в работу.
/// InProgress — админ принял в работу. Resolved — закрыт, требует
/// resolution_note.
/// </summary>
public enum SupportTicketStatus
{
    Unknown = 0,
    Open = 1,
    InProgress = 2,
    Resolved = 3
}
