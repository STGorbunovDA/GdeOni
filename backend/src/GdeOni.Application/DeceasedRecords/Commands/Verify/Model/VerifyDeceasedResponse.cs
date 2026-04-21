namespace GdeOni.Application.DeceasedRecords.Commands.Verify.Model;

public sealed record VerifyDeceasedResponse(
    Guid DeceasedId,
    bool IsVerified);