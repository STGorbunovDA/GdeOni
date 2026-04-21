namespace GdeOni.Application.DeceasedRecords.Commands.Unverified.Model;

public sealed record UnverifiedDeceasedResponse(
    Guid DeceasedId,
    bool IsVerified);