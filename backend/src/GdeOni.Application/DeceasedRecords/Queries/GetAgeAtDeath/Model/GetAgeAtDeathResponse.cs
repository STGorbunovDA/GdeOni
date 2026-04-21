namespace GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.Model;

public sealed record GetAgeAtDeathResponse(
    Guid DeceasedId,
    int? AgeAtDeath);