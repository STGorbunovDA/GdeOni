namespace GdeOni.API.Security;

/// <summary>
/// Имена кастомных claim'ов в JWT, не входящих в стандартный набор.
/// </summary>
public static class JwtClaimNames
{
    /// <summary>
    /// Метка для инвалидации токена при смене пароля/роли/email.
    /// Сверяется с User.SecurityStamp в JwtBearerEvents.OnTokenValidated.
    /// </summary>
    public const string SecurityStamp = "stamp";
}
