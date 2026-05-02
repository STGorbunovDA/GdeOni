namespace GdeOni.API.Security;

public static class JwtClaimNames
{
    /// <summary>
    /// Метка для инвалидации токена при смене пароля/роли/email.
    /// Сверяется с User.SecurityStamp в JwtBearerEvents.OnTokenValidated.
    /// </summary>
    public const string SecurityStamp = "stamp";
}
