using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Support;

public sealed class UpdateSupportTicketSeverityRequest
{
    public SupportTicketSeverity Severity { get; set; }
}
