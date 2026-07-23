namespace GdeOni.API.Models.DeceasedRecords;

/// <summary>
/// Запрос добавления воспоминания (memory) к карточке умершего.
/// </summary>
public sealed class AddMemoryRequest
{
    /// <summary>Текст воспоминания.</summary>
    public string Text { get; set; } = null!;
}
