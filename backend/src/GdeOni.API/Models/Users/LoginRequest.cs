namespace GdeOni.API.Models.Users;

/// <summary>
/// Запрос входа в систему: email (или логин) и пароль.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Email ИЛИ логин пользователя. Имя поля осталось <c>email</c> ради
    /// совместимости с уже выпущенными клиентами (web/мобилка шлют его),
    /// но сервер принимает и логин.
    /// </summary>
    public string Email { get; set; } = null!;
    /// <summary>Пароль пользователя.</summary>
    public string Password { get; set; } = null!;
}
