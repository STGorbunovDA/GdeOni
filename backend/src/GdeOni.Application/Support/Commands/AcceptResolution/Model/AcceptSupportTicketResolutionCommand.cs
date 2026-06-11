namespace GdeOni.Application.Support.Commands.AcceptResolution.Model;

/// <summary>
/// D25. Юзер закрепляет резолюцию админа ("Закрепить решено"). UserId
/// берётся из JWT — в DTO его нет, чтобы нельзя было "закрепить за
/// другого".
/// </summary>
public record AcceptSupportTicketResolutionCommand(Guid TicketId);
