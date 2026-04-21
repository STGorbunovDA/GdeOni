namespace GdeOni.API.Models.DeceasedRecords;

public sealed class GetAllDeceasedRequest
{
    public string? Search { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public bool? IsVerified { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}