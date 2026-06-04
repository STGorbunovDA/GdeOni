namespace GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.Model;

public sealed record GetUserTrackedDeceasedForAdminQuery(
    Guid UserId,
    int Page,
    int PageSize);
