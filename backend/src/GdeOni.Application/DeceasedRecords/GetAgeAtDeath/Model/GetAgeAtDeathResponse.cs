namespace GdeOni.Application.DeceasedRecords.GetAgeAtDeath.Model;

public sealed record GetAgeAtDeathResponse(
    Guid DeceasedId,
    int? AgeAtDeath);