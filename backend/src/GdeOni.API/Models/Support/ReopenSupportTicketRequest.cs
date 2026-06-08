namespace GdeOni.API.Models.Support;

public sealed class ReopenSupportTicketRequest
{
    /// <summary>Сообщение юзера админу при reopen. Опционально.</summary>
    public string? UserReply { get; set; }
}
