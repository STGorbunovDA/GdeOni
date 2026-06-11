namespace GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;

public sealed record GetAllDeceasedQuery(
    string? Search,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    string? Country,
    string? City,
    bool? IsVerified,
    DateTime? CreatedFrom,
    DateTime? CreatedTo,
    DateOnly? BirthDate,
    DateOnly? DeathDate,
    int Page,
    int PageSize);