using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Persistence;

/// <summary>
/// D25. Репозиторий обращений в службу поддержки. Save() — общая
/// UoW-граница со всеми остальными репозиториями: один <c>AppDbContext</c>,
/// одна транзакция per use-case. Это важно для auto-инцидентов: тикет
/// и связанная мутация (например, MarkFailed для платежа) должны
/// коммититься атомарно.
/// </summary>
public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// D25.2. Аналог GetById с подгрузкой Messages (Include). Для
    /// карточки тикета (юзер/админ) — там нужна вся переписка.
    /// </summary>
    Task<SupportTicket?> GetByIdWithMessages(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// D33. Облегчённый аналог GetByIdWithMessages — подгружает
    /// только Attachments. Используется для скачивания вложения
    /// (нет смысла тянуть messages в этот сценарий).
    /// </summary>
    Task<SupportTicket?> GetByIdWithAttachments(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Пагинированный список для админ-UI с фильтрами. statuses /
    /// severities — массивы для чек-боксного UI (несколько статусов
    /// одновременно). emailSearch — частичное совпадение email юзера
    /// (ILIKE), удобнее чем UUID.
    /// </summary>
    Task<(List<(SupportTicket Ticket, string? UserEmail)> Items, int TotalCount)> GetPagedForAdmin(
        Guid? userId,
        SupportTicketStatus[]? statuses,
        SupportTicketSeverity[]? severities,
        SupportTicketKind? kind,
        SupportTicketSource? source,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// "Мои обращения" — только тикеты текущего юзера, отсортированы
    /// по CreatedAtUtc DESC.
    /// </summary>
    Task<(List<SupportTicket> Items, int TotalCount)> GetPagedForUser(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task Add(SupportTicket ticket, CancellationToken cancellationToken);
    Task Save(CancellationToken cancellationToken);
}
