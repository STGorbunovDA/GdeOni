namespace GdeOni.Application.Users.Commands.AdminRemoveUserTracking.Model;

public sealed record AdminRemoveUserTrackingCommand(Guid UserId, Guid DeceasedId);

public sealed record AdminRemoveAllUserTrackingCommand(Guid UserId);

public sealed record AdminRemoveAllUserTrackingResponse(int RemovedCount);
