using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.Common;

/// <summary>
/// Универсальный DTO тикета. Используется и в листинге, и в карточке
/// (admin/user views). <c>UserEmail</c> заполняется только в админских
/// query — юзеру свой email не нужен.
/// </summary>
public sealed class SupportTicketDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }

    public SupportTicketSource Source { get; set; }
    public SupportTicketKind Kind { get; set; }
    public SupportTicketSeverity Severity { get; set; }
    public SupportTicketStatus Status { get; set; }

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Details { get; set; }
    public string? ResolutionNote { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }

    public bool AcceptedByUser { get; set; }
    public DateTime? AcceptedByUserAtUtc { get; set; }
    public string? LastUserReply { get; set; }
    public DateTime? LastUserReplyAtUtc { get; set; }
    public int ReopenedCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// D25.2. История переписки в хронологическом порядке ASC.
    /// Заполняется только в GetById query, в листинге null.
    /// </summary>
    public List<SupportTicketMessageDto>? Messages { get; set; }
}
