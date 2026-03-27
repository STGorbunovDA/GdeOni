namespace GdeOni.Application.DeceasedRecords.Update.Model;

public sealed class UpdateDeceasedRequest
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? MiddleName { get; set; }

    public DateTime? BirthDate { get; set; }
    public DateTime DeathDate { get; set; }

    public string? ShortDescription { get; set; }
    public string? Biography { get; set; }

    public UpdateDeceasedBurialLocationDto BurialLocation { get; set; } = null!;
    public UpdateDeceasedMetadataDto? Metadata { get; set; }
}