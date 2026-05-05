using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;

public sealed record GetMyTrackedDeceasedDetailsResult(
    Deceased Deceased,
    TrackedDeceased Tracking,
    bool CanSeeAllMemories);
