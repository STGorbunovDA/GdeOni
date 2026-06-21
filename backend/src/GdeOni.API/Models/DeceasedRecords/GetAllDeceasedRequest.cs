namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Параметры выборки карточек умерших с пагинацией и фильтрацией.
/// </summary>
public sealed class GetAllDeceasedRequest
{
    /// <summary>
    /// Legacy "any field" — ILike по FirstName/LastName/MiddleName
    /// одной строкой. Оставлено для обратной совместимости.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// E17.5: точечный поиск по имени (ILike substring).
    /// AND с LastName/MiddleName если переданы.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// E17.5: точечный поиск по фамилии.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// E17.5: точечный поиск по отчеству.
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>Фильтр по стране захоронения.</summary>
    public string? Country { get; set; }
    /// <summary>Фильтр по городу захоронения.</summary>
    public string? City { get; set; }
    /// <summary>Фильтр по флагу верификации карточки администратором.</summary>
    public bool? IsVerified { get; set; }
    /// <summary>Нижняя граница периода создания карточки (включительно, UTC).</summary>
    public DateTime? CreatedFrom { get; set; }
    /// <summary>Верхняя граница периода создания карточки (включительно, UTC).</summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Точное совпадение даты рождения. Используется в поиске
    /// перед добавлением (E16), чтобы отличить тёзку от нужного
    /// умершего. BirthDate в Deceased — nullable, поэтому если
    /// в фильтре указано значение, а у карточки BirthDate=null,
    /// она НЕ попадёт в выдачу.
    /// </summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>
    /// Точное совпадение даты смерти. DeathDate всегда есть на
    /// карточке (required в Domain), поэтому фильтр всегда работает.
    /// </summary>
    public DateOnly? DeathDate { get; set; }

    /// <summary>Номер страницы (от 1).</summary>
    public int Page { get; set; } = 1;
    /// <summary>Размер страницы.</summary>
    public int PageSize { get; set; } = 20;
}
