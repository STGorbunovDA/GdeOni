namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// D24. Тело PATCH /api/deceased-records/{id}/main-info.
/// </summary>
public sealed record UpdateMainInfoByEditorRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    string? ShortDescription,
    string? Biography);
