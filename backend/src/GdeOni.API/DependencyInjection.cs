using System.Security.Claims;
using System.Text;
using GdeOni.API.Authorization;
using GdeOni.API.Security;
using GdeOni.Application.Common.Security;
using GdeOni.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GdeOni.API;

/// <summary>
/// Регистрация CORS и Security/Authorization для приложения API.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Имя CORS-политики, прикрепляемой к pipeline.</summary>
    public const string CorsPolicyName = "GdeOniCors";

    private static readonly string[] DefaultDevOrigins =
    [
        "http://localhost:5173",
        "http://localhost:3000"
    ];

    /// <summary>
    /// Регистрирует CORS-политику: список origins берётся из конфигурации,
    /// в Development допускается fallback на localhost.
    /// </summary>
    public static IServiceCollection AddCustomCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var configuredOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>();

        string[] origins;
        if (configuredOrigins is { Length: > 0 })
        {
            origins = configuredOrigins;
        }
        else if (environment.IsDevelopment())
        {
            // Development: молчаливый fallback на localhost — частый сценарий
            // локальной разработки с двумя стандартными портами фронта.
            origins = DefaultDevOrigins;
        }
        else
        {
            // Production / Staging: fail-fast вместо silent breakage
            // прод-фронта (CORS-блок без логов). См. D7.64.
            throw new InvalidOperationException(
                "Cors:AllowedOrigins не сконфигурирован. " +
                "В non-Development среде секция обязательна.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                // Явный whitelist headers/methods вместо AllowAnyHeader/Method.
                // Вместе с AllowCredentials комбинация "ANY + credentials"
                // раньше давала источнику из whitelist полный доступ от
                // имени юзера: XSS на самом фронте или subdomain-takeover
                // = CSRF без ограничений. Теперь только то, что мы реально
                // используем.
                policy
                    .WithOrigins(origins)
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                    .WithHeaders(
                        "Authorization",
                        "Content-Type",
                        "Accept",
                        "X-Requested-With",
                        "X-Trace-Id")
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Регистрирует JWT-аутентификацию, политики авторизации и
    /// security-сервисы (CurrentUserService, JwtProvider, SecurityStampInvalidator).
    /// </summary>
    public static IServiceCollection AddSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // D43. Восстановление пароля. Секция опциональна: без неё
        // (WebResetUrl пуст) письма не отправляются, но приложение
        // стартует — как и с невыключенным SMTP.
        services.Configure<PasswordResetOptions>(
            configuration.GetSection(PasswordResetOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                         ?? throw new InvalidOperationException("JWT settings are not configured.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
            throw new InvalidOperationException("JWT issuer is not configured.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
            throw new InvalidOperationException("JWT audience is not configured.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
            throw new InvalidOperationException("JWT secret key is not configured.");

        // HMAC-SHA256 требует ключ ≥ 32 байта (256 бит). Без проверки
        // короткий ключ либо роняет Login на первом обращении, либо
        // (на старых версиях SymmetricSecurityKey) даёт слабую подпись.
        // Fail-fast на старте, как уже сделано для Issuer/Audience. См. D7.65.
        var secretKeyByteCount = Encoding.UTF8.GetByteCount(jwtOptions.SecretKey);
        if (secretKeyByteCount < 32)
            throw new InvalidOperationException(
                $"JWT secret key слишком короткий: {secretKeyByteCount} байт, требуется минимум 32 (HMAC-SHA256).");

        // Дополнительный entropy-чек (D11.7.3): UTF-8 длина не отражает
        // реальный объём энтропии — секрет "aaaaaaaaaa...aa" формально
        // даст 32 байта. Мы требуем минимум 16 уникальных символов,
        // что грубо отсеивает явный мусор. Полноценную энтропию (Shannon)
        // на старте мерять — overkill; правильный путь — генерировать
        // секрет через `openssl rand -base64 32`, и это документируется
        // в README/deploy.md. Код ниже — последний рубеж.
        var distinctChars = jwtOptions.SecretKey.Distinct().Count();
        if (distinctChars < 16)
            throw new InvalidOperationException(
                $"JWT secret key выглядит низкоэнтропийным: только {distinctChars} уникальных символов. " +
                "Сгенерируй ключ через `openssl rand -base64 32` или эквивалент.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    // После проверки подписи и срока действия — сверяем
                    // SecurityStamp из токена с актуальным значением в БД.
                    // При смене пароля/роли/email Domain.User инкрементирует
                    // stamp, и все ранее выпущенные токены становятся
                    // невалидны на следующем же запросе.
                    OnTokenValidated = ValidateSecurityStampAsync
                };
            });

        // D16. Default-политика = аутентификация + активная подписка.
        // Применяется ко всем [Authorize] без указания policy.
        // Whitelist'овые эндпоинты помечаются
        // [Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)] —
        // только аутентификация без подписки.
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.BasicAuthenticated, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(AuthorizationPolicies.RequireActiveSubscription, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new ActiveSubscriptionRequirement()));

            options.DefaultPolicy = options.GetPolicy(
                AuthorizationPolicies.RequireActiveSubscription)!;
        });
        services.AddSingleton<IAuthorizationHandler, ActiveSubscriptionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, SubscriptionAwareAuthorizationResultHandler>();

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        // Без состояния — IMemoryCache внутри сам Singleton (D11.8.1).
        services.AddSingleton<ISecurityStampInvalidator, SecurityStampInvalidator>();

        return services;
    }

    internal static string SecurityStampCacheKey(Guid userId) => $"secstamp:{userId}";

    /// <summary>
    /// Кеш-ключ для read-through ActiveSubscriptionAuthorizationHandler:
    /// "юзер X имеет активный access (subscription или complimentary)".
    /// Инвалидируется тем же SecurityStampInvalidator — любая операция,
    /// меняющая статус подписки, должна ротировать SecurityStamp юзера.
    /// </summary>
    internal static string SubscriptionAccessCacheKey(Guid userId) => $"subaccess:{userId}";

    private static async Task ValidateSecurityStampAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal is null)
        {
            context.Fail("Token has no principal.");
            return;
        }

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var stampClaim = principal.FindFirstValue(JwtClaimNames.SecurityStamp);

        if (!Guid.TryParse(userIdClaim, out var userId)
            || !Guid.TryParse(stampClaim, out var tokenStamp))
        {
            context.Fail("Token claims malformed.");
            return;
        }

        // Read-through кеш с коротким TTL — без него SELECT security_stamp
        // FROM users WHERE id=@uid идёт на КАЖДОМ аутентифицированном запросе.
        // Trade-off задокументирован в JwtOptions.SecurityStampCacheTtlSeconds.
        //
        // SecurityStampStrictMode=true пропускает кеш — каждый запрос SELECT.
        // Используется когда security-аудит требует нулевого окна
        // invalidation при Block/ChangePassword.
        var sp = context.HttpContext.RequestServices;
        var cache = sp.GetRequiredService<IMemoryCache>();
        var jwtOptions = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
        var cacheKey = SecurityStampCacheKey(userId);

        Guid? cachedStamp;
        if (jwtOptions.SecurityStampStrictMode || !cache.TryGetValue<Guid?>(cacheKey, out cachedStamp))
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            cachedStamp = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => (Guid?)u.SecurityStamp)
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

            // В StrictMode кеш не используем — каждый запрос идёт в БД.
            // Иначе сохраняем результат в кеш.
            if (!jwtOptions.SecurityStampStrictMode)
            {
                cache.Set(
                    cacheKey,
                    cachedStamp,
                    TimeSpan.FromSeconds(jwtOptions.SecurityStampCacheTtlSeconds));
            }
        }

        if (cachedStamp is null || cachedStamp.Value != tokenStamp)
        {
            context.Fail("Security stamp mismatch.");
        }
    }
}