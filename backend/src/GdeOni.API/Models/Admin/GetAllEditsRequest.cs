namespace GdeOni.API.Models.Admin;

/// <summary>
/// D24/F17.9. Query-параметры для админ-ленты правок. Пагинация
/// и диапазон валидируются в GetAllEditsQueryValidator (Page>=1,
/// PageSize 1..200, From&lt;=To).
/// </summary>
public sealed class GetAllEditsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public Guid? DeceasedId { get; set; }
    public Guid? EditorUserId { get; set; }
    public DateTime? EditedFromUtc { get; set; }
    public DateTime? EditedToUtc { get; set; }
}

/// <summary>
/// Пагинация для истории правок одной карточки.
/// </summary>
public sealed class GetDeceasedEditsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
