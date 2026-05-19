namespace GdeOni.Application.Users.Queries.GetCurrent.Model;

public sealed class GetCurrentUserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string? FullName { get; init; }
    public string Role { get; init; } = null!;

    /// <summary>
    /// D19. Версия Privacy Policy, принятая пользователем. 0 = не принято
    /// (исторические записи до D19 migration). Клиент сравнивает с
    /// <c>GET /api/legal/privacy-policy</c>.Version: если меньше →
    /// показать модалку "Правила обновлены, примите".
    /// </summary>
    public int PrivacyPolicyVersion { get; init; }

    /// <summary>
    /// D19. Версия Terms of Use, принятая пользователем.
    /// </summary>
    public int TermsVersion { get; init; }

    /// <summary>
    /// D19. true если PrivacyPolicyVersion или TermsVersion меньше
    /// текущей серверной. Удобный флаг чтобы клиент не тащил
    /// сравнения сам.
    /// </summary>
    public bool HasOutdatedLegalAcceptance { get; init; }
}
