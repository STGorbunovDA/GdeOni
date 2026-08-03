namespace GdeOni.API.Models.Relatives;

/// <summary>Тело POST /api/relatives/reports — пожаловаться на собеседника.</summary>
public sealed class ReportRelativeRequest
{
    /// <summary>Диалог, в контексте которого подаётся жалоба.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>Текст жалобы (что не так).</summary>
    public string Reason { get; set; } = null!;
}

/// <summary>Тело POST /api/admin/relative-reports/{id}/resolve.</summary>
public sealed class ResolveRelativeReportRequest
{
    /// <summary>Необязательная пометка админа о решении.</summary>
    public string? Note { get; set; }
}
