namespace GdeOni.API.Models.Users;

public sealed class RegisterUserRequest
{
    public string Email { get; set; } = null!;
    public string? UserName { get; set; }
    public string? FullName { get; set; }
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
}
