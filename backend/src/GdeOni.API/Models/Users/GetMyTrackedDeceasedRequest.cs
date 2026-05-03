namespace GdeOni.API.Models.Users;

public sealed class GetMyTrackedDeceasedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
