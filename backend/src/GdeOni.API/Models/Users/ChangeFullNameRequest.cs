namespace GdeOni.API.Models.Users;

/// <summary>
/// Смена полного имени (ФИО). Не уникально — тёзки допустимы.
/// </summary>
public sealed class ChangeFullNameRequest
{
    /// <summary>Полное имя. null или пустая строка очищает поле.</summary>
    public string? FullName { get; set; }
}
