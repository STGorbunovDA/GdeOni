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
    /// Use case'ы ChangeEmail / ChangePassword / ChangeRole / UpdateProfile /
    /// DeleteUser сбрасывают кеш через ISecurityStampInvalidator сразу после
    /// Save (D11.8.1, D11.10.1) — для этих сценариев окно компрометации
    /// нулевое.
    /// TTL остаётся актуальным для:
    ///   1) прямых мутаций User в БД минуя use case (миграции, manual SQL);
    ///   2) multi-instance деплоя — IMemoryCache локальный, не распределённый,
    ///      другие реплики увидят новый stamp через БД-проверку только
    ///      после истечения TTL.
    /// 30 секунд — компромисс между нагрузкой на БД (без кеша SELECT
    /// на каждом запросе) и временем реакции в этих остаточных сценариях.
    /// </summary>
    public int SecurityStampCacheTtlSeconds { get; set; } = 30;
}
