using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Support;

/// <summary>
/// Запрос изменения уровня критичности обращения в поддержку (админская операция).
/// </summary>
public sealed class UpdateSupportTicketSeverityRequest
{
    /// <summary>Новый уровень критичности обращения.</summary>
    public SupportTicketSeverity Severity { get; set; }
}
