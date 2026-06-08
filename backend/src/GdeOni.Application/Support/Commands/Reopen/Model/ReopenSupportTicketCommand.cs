namespace GdeOni.Application.Support.Commands.Reopen.Model;

/// <summary>
/// D25. Юзер не согласен с резолюцией ("Продолжить спор"). UserReply
/// опционален — UI принуждает заполнить, но домен пропускает пустой
/// (бывают сценарии "просто кнопка переоткрыть").
/// </summary>
public record ReopenSupportTicketCommand(Guid TicketId, string? UserReply);
