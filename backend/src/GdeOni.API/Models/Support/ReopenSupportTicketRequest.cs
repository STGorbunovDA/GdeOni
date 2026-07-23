namespace GdeOni.API.Models.Support;

/// <summary>
/// Запрос повторного открытия (reopen) закрытого обращения в поддержку
/// пользователем.
/// </summary>
public sealed class ReopenSupportTicketRequest
{
    /// <summary>Сообщение юзера админу при reopen. Опционально.</summary>
    public string? UserReply { get; set; }
}
