using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Параметры выборки медиа-файлов (фото/документы) карточки умершего
/// с пагинацией и фильтрацией.
/// </summary>
public sealed class GetMediaListRequest
{
    /// <summary>Тип медиа (фото/документ); null — без фильтра.</summary>
    public MediaKind? Kind { get; set; }
    /// <summary>Статус модерации; null — без фильтра.</summary>
    public ModerationStatus? ModerationStatus { get; set; }
    /// <summary>Номер страницы (от 1).</summary>
    public int Page { get; set; } = 1;
    /// <summary>Размер страницы.</summary>
    public int PageSize { get; set; } = 20;
}
