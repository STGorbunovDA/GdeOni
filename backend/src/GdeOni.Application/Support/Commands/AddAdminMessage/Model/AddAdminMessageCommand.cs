namespace GdeOni.Application.Support.Commands.AddAdminMessage.Model;

/// <summary>
/// D44. Ответ админа в переписку обращения БЕЗ смены статуса.
/// Раньше написать можно было только через резолюцию — чтобы задать
/// уточняющий вопрос, приходилось помечать обращение решённым.
/// </summary>
public sealed record AddAdminMessageCommand(Guid TicketId, string Text);
