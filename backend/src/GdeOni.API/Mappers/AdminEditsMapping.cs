using GdeOni.API.Models.Admin;
using GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.Model;

namespace GdeOni.API.Mappers;

/// <summary>
/// D24/F17.9. Request → Query маппинг для админ-ленты правок. Inline
/// clamping (page&lt;=0 ? 1 : page) удалён — пагинация теперь валидируется
/// в Get*Validator'ах, контроллер просто передаёт значения как есть.
/// </summary>
public static class AdminEditsMapping
{
    /// <summary>Маппит DTO админ-ленты правок в запрос use case.</summary>
    public static GetAllEditsQuery ToQuery(this GetAllEditsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new GetAllEditsQuery(
            request.Page,
            request.PageSize,
            request.DeceasedId,
            request.EditorUserId,
            request.EditedFromUtc,
            request.EditedToUtc);
    }

    /// <summary>Маппит DTO истории правок одной карточки в запрос use case.</summary>
    public static GetDeceasedEditsQuery ToQuery(this GetDeceasedEditsRequest request, Guid deceasedId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new GetDeceasedEditsQuery(deceasedId, request.Page, request.PageSize);
    }
}
