namespace GdeOni.Application.DeceasedRecords.Commands.ApproveMediaModeration.Model;

public sealed record ApproveMediaModerationCommand(Guid DeceasedId, Guid MediaId);
