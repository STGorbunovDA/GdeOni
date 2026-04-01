namespace GdeOni.Application.DeceasedRecords.Unverify.Model;

public sealed record UnverifyDeceasedResponse(
    Guid DeceasedId,
    bool IsVerified);