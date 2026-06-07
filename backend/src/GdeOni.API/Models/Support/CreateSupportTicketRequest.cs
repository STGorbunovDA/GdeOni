using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Support;

public sealed class CreateSupportTicketRequest
{
    public SupportTicketKind Kind { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
}
