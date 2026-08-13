namespace GdeOni.Application.Users.Queries.GetCurrent.Model;

public sealed class GetCurrentUserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string UserName { get; init; } = null!;

    /// <summary>
    /// Уникальный логин для входа (наравне с email). Показывается в профиле,
    /// чтобы человек знал, чем он может войти.
    /// </summary>
    public string Login { get; init; } = null!;

    public string? FullName { get; init; }

    /// <summary>
    /// Город пользователя. null/пусто → клиент показывает баннер-напоминание
    /// «укажите город» и даёт заполнить в профиле.
    /// </summary>
    public string? City { get; init; }

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

    /// <summary>
    /// D45. Подтверждён ли email. Внутрь приложения неподтверждёнными
    /// попадают только «старые» пользователи (новых до подтверждения
    /// не пускает гейт входа) — клиент по false показывает баннер
    /// «Подтвердите email».
    /// </summary>
    public bool IsEmailConfirmed { get; init; }

    /// <summary>
    /// Функция «Родственники»: согласие быть видимым как родственник и
    /// получать сообщения (по умолчанию true). Клиент показывает
    /// переключатель в профиле; false — пользователь скрыт из чужих списков
    /// родственников и ему нельзя написать.
    /// </summary>
    public bool AllowRelativeConnections { get; init; }
}
