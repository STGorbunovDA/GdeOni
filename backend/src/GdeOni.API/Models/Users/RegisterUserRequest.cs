namespace GdeOni.API.Models.Users;

/// <summary>
/// Запрос регистрации нового пользователя с обязательным согласием на
/// Privacy Policy и Terms of Use.
/// </summary>
public sealed class RegisterUserRequest
{
    /// <summary>Email пользователя.</summary>
    public string Email { get; set; } = null!;
    /// <summary>Логин пользователя (опционально).</summary>
    public string? UserName { get; set; }
    /// <summary>Полное имя пользователя (опционально).</summary>
    public string? FullName { get; set; }
    /// <summary>Пароль пользователя.</summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// D19. Согласие с Privacy Policy. Регистрация без явного
    /// <c>true</c> отклоняется валидатором.
    /// </summary>
    public bool PrivacyPolicyAccepted { get; set; }

    /// <summary>
    /// D19. Согласие с Terms of Use. Аналогично.
    /// </summary>
    public bool TermsAccepted { get; set; }

    /// <summary>
    /// D19. Дата рождения пользователя. Обязательное поле для новой
    /// регистрации: сервис не предоставляется лицам младше
    /// <c>User.MinAllowedAge</c> лет (Условия использования, п. 3.4).
    /// Формат — ISO date (yyyy-MM-dd).
    /// </summary>
    public DateOnly BirthDate { get; set; }
}
