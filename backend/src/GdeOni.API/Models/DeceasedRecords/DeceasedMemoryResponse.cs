namespace GdeOni.API.Models.DeceasedRecords;

public sealed class DeceasedMemoryResponse
{
    public Guid Id { get; init; }
    public string Text { get; init; } = null!;
    public Guid? AuthorUserId { get; init; }

    /// <summary>
    /// Отображаемое имя автора — <c>FullName ?? UserName</c>. Null если
    /// юзер удалён или AuthorUserId не существует. UI отрисует «Аноним».
    /// Заполняется в use case через batch-вызов
    /// <c>IUserRepository.GetDisplayNamesByIds</c> чтобы избежать N+1.
    /// </summary>
    public string? AuthorName { get; init; }

    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public string ModerationStatus { get; init; } = null!;
}
