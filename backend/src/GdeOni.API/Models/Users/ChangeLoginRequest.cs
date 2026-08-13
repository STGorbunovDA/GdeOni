namespace GdeOni.API.Models.Users;

/// <summary>
/// Смена собственного логина. Логин уникален: если занят другим
/// пользователем, сервер вернёт 409 <c>user.login.already.exists</c>.
/// </summary>
public sealed class ChangeLoginRequest
{
    /// <summary>
    /// Новый логин: латиница, цифры и <c>. _ - + @</c>. Полный email тоже
    /// допустим — так разводятся одинаковые префиксы почты.
    /// </summary>
    public string Login { get; set; } = null!;
}
