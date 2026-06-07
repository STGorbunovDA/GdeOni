using System.Threading.RateLimiting;
using GdeOni.API.Response;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.RateLimiting;

namespace GdeOni.API.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCustomRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RateLimitingOptions>(
            configuration.GetSection(RateLimitingOptions.SectionName));

        var options = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        var auth = options.Auth;
        var webhook = options.Webhook;

        services.AddRateLimiter(rl =>
        {
            // Sliding window per-IP. Партиционирование по
            // Connection.RemoteIpAddress — после ForwardedHeaders (D7.38)
            // это уже client IP, не proxy.
            //
            // KNOWN LIMITATION (CGNAT/корп-NAT): десятки реальных юзеров
            // за одним IP делят лимит. На школьном/корп-выходе PermitLimit=10
            // может схватываться быстро. Полный фикс — партиционировать
            // по (email из body + IP), для этого нужен middleware который
            // читает body Login. Backlog "RateLimiter CGNAT".
            rl.AddPolicy(AuthRateLimitOptions.PolicyName, httpContext =>
            {
                var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: key,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = auth.PermitLimit,
                        Window = TimeSpan.FromMinutes(auth.WindowMinutes),
                        SegmentsPerWindow = auth.SegmentsPerWindow,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            // Лимит для webhook-эндпоинтов. Партиция по IP — даже без
            // IP-whitelist (см. WebhookIpWhitelistMiddleware) атакующий
            // не сможет завалить нас потоком webhook'ов с одного host'а.
            rl.AddPolicy(WebhookRateLimitOptions.PolicyName, httpContext =>
            {
                var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: key,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = webhook.PermitLimit,
                        Window = TimeSpan.FromMinutes(webhook.WindowMinutes),
                        SegmentsPerWindow = webhook.SegmentsPerWindow,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            // Кастомный rejected-handler: выдаём ApiResponse с тем же
            // контрактом, что и остальные ошибки, плюс Retry-After.
            rl.OnRejected = async (ctx, ct) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    ctx.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                var error = Errors.General.TooManyRequests();
                await ctx.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse<object?>.Error(error),
                    ct);
            };
        });

        return services;
    }
}
