namespace GdeOni.API.Models.Admin;

/// <summary>
/// D24/F17.9. Query-параметры для админ-ленты правок. Пагинация
/// и диапазон валидируются в GetAllEditsQueryValidator (Page>=1,
/// PageSize 1..200, From&lt;=To).
/// </summary>
public sealed class GetAllEditsRequest
{
    /// <summary>Номер страницы (от 1).</summary>
    public int Page { get; set; } = 1;
    /// <summary>Размер страницы.</summary>
    public int PageSize { get; set; } = 50;
    /// <summary>Фильтр по идентификатору карточки умершего.</summary>
    public Guid? DeceasedId { get; set; }
    /// <summary>Фильтр по идентификатору пользователя-редактора.</summary>
    public Guid? EditorUserId { get; set; }
    /// <summary>Нижняя граница периода правки в UTC (включительно).</summary>
    public DateTime? EditedFromUtc { get; set; }
    /// <summary>Верхняя граница периода правки в UTC (включительно).</summary>
    public DateTime? EditedToUtc { get; set; }
}

/// <summary>
/// Пагинация для истории правок одной карточки.
/// </summary>
public sealed class GetDeceasedEditsRequest
{
    /// <summary>Номер страницы (от 1).</summary>
    public int Page { get; set; } = 1;
    /// <summary>Размер страницы.</summary>
    public int PageSize { get; set; } = 50;
}
