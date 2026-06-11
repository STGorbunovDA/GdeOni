namespace GdeOni.API.Models.Users;

/// <summary>
/// D19. Тело запроса <c>POST /api/users/me/accept-legal</c>.
/// </summary>
public sealed class AcceptLegalRequest
{
    /// <summary>
    /// Версия Privacy Policy, с которой пользователь соглашается.
    /// Должна быть равна текущей серверной (иначе 409 legal.version.outdated).
    /// </summary>
    public int PrivacyPolicyVersion { get; set; }

    /// <summary>
    /// Версия Terms of Use, с которой пользователь соглашается.
    /// </summary>
    public int TermsVersion { get; set; }
}
