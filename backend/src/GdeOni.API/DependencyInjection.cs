using System.Text;
using GdeOni.API.Security;
using GdeOni.Application.Common.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GdeOni.API;

public static class DependencyInjection
{
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
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}