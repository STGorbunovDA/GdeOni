namespace GdeOni.Application.Relatives.Commands.ReportRelative.Model;

/// <summary>
/// Жалоба на собеседника в контексте диалога (Фаза 5). Кто нарушитель —
/// вычисляется из диалога (второй участник), поэтому в команде только id
/// диалога и текст жалобы.
/// </summary>
public sealed record ReportRelativeCommand(Guid ConversationId, string Reason);

/// <summary>Created=false — активная жалоба на этого человека уже была (дедуп).</summary>
public sealed record ReportRelativeResponse(bool Created);
