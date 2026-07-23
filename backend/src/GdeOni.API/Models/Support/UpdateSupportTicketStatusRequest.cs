using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Support;

/// <summary>
/// Запрос изменения статуса обращения в поддержку (админская операция).
/// </summary>
public sealed class UpdateSupportTicketStatusRequest
{
    /// <summary>Новый статус обращения.</summary>
    public SupportTicketStatus Status { get; set; }
    /// <summary>Заметка о решении (отображается пользователю).</summary>
    public string? ResolutionNote { get; set; }
}
