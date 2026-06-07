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

    /// <summary>
    /// StrictMode: при true кеш SecurityStamp в OnTokenValidated
    /// отключается — каждый authenticated-запрос делает SELECT по users.
    /// Окно invalidation после Block/ChangePassword/ChangeRole становится
    /// нулевым, но нагрузка на БД растёт линейно с RPS.
    ///
    /// Включай при жёстких security-аудитах ("сессия должна закрываться
    /// мгновенно при смене пароля") и достаточном PG-headroom. По
    /// умолчанию false — стандартный режим с 30-сек окном.
    /// </summary>
    public bool SecurityStampStrictMode { get; set; } = false;
}
