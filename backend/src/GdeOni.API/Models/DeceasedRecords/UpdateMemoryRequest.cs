namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Запрос изменения текста существующего воспоминания (memory).
/// </summary>
public sealed class UpdateMemoryRequest
{
    /// <summary>Новый текст воспоминания.</summary>
    public string Text { get; set; } = null!;
}
