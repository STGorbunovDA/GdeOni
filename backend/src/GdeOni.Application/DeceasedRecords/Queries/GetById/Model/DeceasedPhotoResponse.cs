namespace GdeOni.Application.DeceasedRecords.Queries.GetById.Model;

public sealed class DeceasedPhotoResponse
{
    public Guid Id { get; init; }
    public string Url { get; init; } = null!;
    public string? Description { get; init; }
    public bool IsPrimary { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public Guid AddedByUserId { get; init; }
    public int ModerationStatus { get; init; }
}