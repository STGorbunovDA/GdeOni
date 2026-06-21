namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Одно воспоминание (memory) на карточке умершего в ответе API.
/// </summary>
public sealed class DeceasedMemoryResponse
{
    /// <summary>Идентификатор воспоминания.</summary>
    public Guid Id { get; init; }
    /// <summary>Текст воспоминания.</summary>
    public string Text { get; init; } = null!;
    /// <summary>Идентификатор автора (null если автор удалён).</summary>
    public Guid? AuthorUserId { get; init; }

    /// <summary>
    /// Отображаемое имя автора — <c>FullName ?? UserName</c>. Null если
    /// юзер удалён или AuthorUserId не существует. UI отрисует «Аноним».
    /// Заполняется в use case через batch-вызов
    /// <c>IUserRepository.GetDisplayNamesByIds</c> чтобы избежать N+1.
    /// </summary>
    public string? AuthorName { get; init; }

    /// <summary>Дата и время создания воспоминания в UTC.</summary>
    public DateTime CreatedAtUtc { get; init; }
    /// <summary>Дата и время последнего изменения в UTC (null если не правилось).</summary>
    public DateTime? UpdatedAtUtc { get; init; }
    /// <summary>Статус модерации воспоминания (строкой из enum ModerationStatus).</summary>
    public string ModerationStatus { get; init; } = null!;
}
