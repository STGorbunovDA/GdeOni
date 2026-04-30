namespace GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.Model;

public sealed record RejectMediaModerationCommand(Guid DeceasedId, Guid MediaId);
