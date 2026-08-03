namespace GdeOni.API.Models.Users;

/// <summary>Тело PATCH /api/users/me/city.</summary>
public sealed class UpdateCityRequest
{
    /// <summary>Город. null или пустая строка — «не указан».</summary>
    public string? City { get; set; }
}
