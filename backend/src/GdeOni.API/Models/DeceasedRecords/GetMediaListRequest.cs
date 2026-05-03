using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.DeceasedRecords;

public sealed class GetMediaListRequest
{
    public MediaKind? Kind { get; set; }
    public ModerationStatus? ModerationStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
