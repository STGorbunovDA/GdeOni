namespace GdeOni.API.Models.Users;

public sealed class UpdateUserProfileRequest
{
    public string UserName { get; set; } = null!;
    public string? FullName { get; set; }

    /// <summary>
    /// Опциональный current password. Если задан — backend проверит
    /// совпадение перед обновлением профиля. Защита от vandalism через
    /// украденный access-токен. Старые клиенты могут не слать.
    /// </summary>
    public string? CurrentPassword { get; set; }
}