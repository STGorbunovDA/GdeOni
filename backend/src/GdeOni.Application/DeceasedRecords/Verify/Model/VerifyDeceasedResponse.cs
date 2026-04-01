namespace GdeOni.Application.DeceasedRecords.Verify.Model;

public sealed record VerifyDeceasedResponse(
    Guid DeceasedId,
    bool IsVerified);