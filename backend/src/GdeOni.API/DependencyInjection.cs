using System.Security.Claims;
using System.Text;
using GdeOni.API.Security;
using GdeOni.Application.Common.Security;
using GdeOni.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GdeOni.API;

public static class DependencyInjection
{
    public const string CorsPolicyName = "GdeOniCors";

    private static readonly string[] DefaultDevOrigins =
    [
        "http://localhost:5173",
        "http://localhost:3000"
    ];

    public static IServiceCollection AddCustomCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configuredOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>();

        var origins = configuredOrigins is { Length: > 0 }
            ? configuredOrigins
            : DefaultDevOrigins;

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                         ?? throw new InvalidOperationException("JWT settings are not configured.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
            throw new InvalidOperationException("JWT issuer is not configured.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
            throw new InvalidOperationException("JWT audience is not configured.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
            throw new InvalidOperationException("JWT secret key is not configured.");

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

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }

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

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        var actualStamp = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => (Guid?)u.SecurityStamp)
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        if (actualStamp is null || actualStamp.Value != tokenStamp)
        {
            context.Fail("Security stamp mismatch.");
        }
    }
}