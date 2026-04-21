namespace GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;

public sealed record GetAllDeceasedQuery(
    string? Search,
    string? Country,
    string? City,
    bool? IsVerified,
    DateTime? CreatedFrom,
    DateTime? CreatedTo,
    int Page,
    int PageSize);