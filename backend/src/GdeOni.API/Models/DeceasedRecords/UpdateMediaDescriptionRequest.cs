namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Запрос изменения текстового описания медиа-файла карточки умершего.
/// </summary>
public sealed class UpdateMediaDescriptionRequest
{
    /// <summary>Новое описание медиа-файла (null или пусто — очистить).</summary>
    public string? Description { get; init; }
}
