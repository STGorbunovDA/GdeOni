namespace GdeOni.Application.DeceasedRecords.Commands.Unverified.Model;

public sealed record UnverifyDeceasedResponse(
    Guid DeceasedId,
    bool IsVerified);