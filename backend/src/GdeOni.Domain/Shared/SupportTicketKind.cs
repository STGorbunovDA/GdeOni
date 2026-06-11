namespace GdeOni.Domain.Shared;

/// <summary>
/// Тематика обращения. Юзер выбирает в форме, auto-тикеты используют
/// контекст-зависимое значение (например, проблема с webhook'ом → Payment).
/// </summary>
public enum SupportTicketKind
{
    Unknown = 0,
    Payment = 1,
    Bug = 2,
    Complaint = 3,
    Question = 4,
    Other = 5
}
