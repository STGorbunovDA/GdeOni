using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Support;

public sealed class UpdateSupportTicketStatusRequest
{
    public SupportTicketStatus Status { get; set; }
    public string? ResolutionNote { get; set; }
}
