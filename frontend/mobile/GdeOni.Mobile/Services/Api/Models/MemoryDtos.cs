namespace GdeOni.Mobile.Services.Api.Models;

public sealed record AddMemoryRequest(string Text);

public sealed record UpdateMemoryRequest(string Text);

public sealed record AddMemoryResponse(Guid MemoryId);

public sealed record UpdateMemoryResponse(Guid MemoryId);

/// <summary>
/// Имена ModerationStatus на backend (D11.1.1 — enum'ы сериализуются строками).
/// </summary>
public static class ModerationStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

/// <summary>
/// Лимиты, синхронизированные с backend
/// (DeceasedMemoryEntry.MaxTextLength = 5000). Если меняется на backend,
/// поправить здесь.
/// </summary>
public static class MemoryLimits
{
    public const int MaxTextLength = 5000;
}
