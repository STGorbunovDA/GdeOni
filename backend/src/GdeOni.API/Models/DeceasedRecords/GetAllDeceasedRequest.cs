namespace GdeOni.API.Models.DeceasedRecords;

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

    public string? Country { get; set; }
    public string? City { get; set; }
    public bool? IsVerified { get; set; }
    public DateTime? CreatedFrom { get; set; }
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

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}