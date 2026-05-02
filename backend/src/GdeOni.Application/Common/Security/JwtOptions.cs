namespace GdeOni.Application.Common.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string SecretKey { get; set; } = null!;

    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 14;

    /// <summary>
    /// TTL (секунды) для кеша SecurityStamp в JwtBearerEvents.OnTokenValidated.
    /// Trade-off: после смены пароля/роли/email старые токены продолжают
    /// проходить валидацию до этого TTL, потому что в кеше ещё лежит старый
    /// stamp. 30 секунд — компромисс между нагрузкой на БД (без кеша SELECT
    /// на каждом запросе) и временем реакции на инвалидацию.
    /// </summary>
    public int SecurityStampCacheTtlSeconds { get; set; } = 30;
}
