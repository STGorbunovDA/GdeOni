namespace GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.Model;

public sealed record GetUserTrackedDeceasedForAdminResponse(
    IReadOnlyList<UserTrackedDeceasedItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record UserTrackedDeceasedItem(
    Guid DeceasedId,
    string FullName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    string RelationshipType,
    string Status,
    DateTime TrackedAtUtc);
