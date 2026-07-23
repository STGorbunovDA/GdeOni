namespace GdeOni.API.Models.Users;

/// <summary>
/// Запрос обновления профиля текущего пользователя.
/// </summary>
public sealed class UpdateUserProfileRequest
{
    /// <summary>Новый логин пользователя.</summary>
    public string UserName { get; set; } = null!;
    /// <summary>Новое полное имя пользователя.</summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Опциональный current password. Если задан — backend проверит
    /// совпадение перед обновлением профиля. Защита от vandalism через
    /// украденный access-токен. Старые клиенты могут не слать.
    /// </summary>
    public string? CurrentPassword { get; set; }
}
