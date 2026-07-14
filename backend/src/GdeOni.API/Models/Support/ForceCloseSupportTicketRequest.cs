namespace GdeOni.API.Models.Support;

/// <summary>
/// D40. Запрос принудительного закрытия обращения (админская операция).
/// </summary>
public sealed class ForceCloseSupportTicketRequest
{
    /// <summary>
    /// Причина закрытия. Обязательна — уходит пользователю в переписку
    /// отдельным сообщением, чтобы он видел, почему обращение закрыли
    /// без его подтверждения.
    /// </summary>
    public string CloseNote { get; set; } = string.Empty;
}
