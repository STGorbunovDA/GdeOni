using GdeOni.Domain.Aggregates.DeceasedRecords;

namespace GdeOni.Application.DeceasedRecords.Queries.GetById.Model;

public sealed record GetDeceasedByIdResult(Deceased Deceased, bool CanSeeAllMemories);
