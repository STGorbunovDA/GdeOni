namespace GdeOni.API.Models.Support;

/// <summary>
/// D44. Сообщение в переписку обращения. Один и тот же DTO для юзера
/// и админа — различие только в эндпоинте и правах.
/// </summary>
public sealed class AddSupportTicketMessageRequest
{
    /// <summary>Текст сообщения.</summary>
    public string Text { get; set; } = null!;
}
