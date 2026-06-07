using System.Net;
using GdeOni.API.Options;
using GdeOni.API.Response;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.API.Middleware;

/// <summary>
/// Опциональный IP-whitelist для webhook-эндпоинтов. Срабатывает только
/// на путях, начинающихся на /api/payments/ (там лежат webhook'и
/// платёжных провайдеров) и только если AllowedCidrs не пуст.
///
/// Раньше: webhook AllowAnonymous + EnableBuffering без ничего, любой
/// мог слать payload и заставить нас делать pull-verify к YooKassa.
/// Теперь: WAF на уровне приложения — отбиваем по IP до того, как
/// payment-provider начнёт работать.
/// </summary>
public sealed class WebhookIpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly List<(IPAddress NetworkAddress, int PrefixLength)> _allowedNetworks;
    private readonly ILogger<WebhookIpWhitelistMiddleware> _logger;

    public WebhookIpWhitelistMiddleware(
        RequestDelegate next,
        IOptions<WebhookSecurityOptions> options,
        ILogger<WebhookIpWhitelistMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _allowedNetworks = ParseCidrs(options.Value.AllowedCidrs, logger);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // No-op: ничего не настроено или путь не webhook.
        if (_allowedNetworks.Count == 0 ||
            !context.Request.Path.StartsWithSegments("/api/payments"))
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress;
        if (clientIp is null || !IsAllowed(clientIp))
        {
            _logger.LogWarning(
                "Webhook отклонён по IP-whitelist: {ClientIp} путь {Path}",
                clientIp, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var error = Errors.General.Forbidden(
                "webhook.ip.not_allowed",
                "Webhook IP is not in the whitelist.");
            await context.Response.WriteAsJsonAsync(ApiResponse<object?>.Error(error));
            return;
        }

        await _next(context);
    }

    private bool IsAllowed(IPAddress clientIp)
    {
        // Нормализация: IPv4-mapped IPv6 (::ffff:1.2.3.4) приводим к v4
        // — иначе CIDR на v4 не сматчится, а у Kestrel за proxy часто
        // именно mapped-форма.
        var normalized = clientIp.IsIPv4MappedToIPv6
            ? clientIp.MapToIPv4()
            : clientIp;

        foreach (var (network, prefixLength) in _allowedNetworks)
        {
            if (network.AddressFamily != normalized.AddressFamily)
                continue;
            if (IsInSubnet(normalized, network, prefixLength))
                return true;
        }
        return false;
    }

    private static bool IsInSubnet(IPAddress ip, IPAddress network, int prefixLength)
    {
        var ipBytes = ip.GetAddressBytes();
        var netBytes = network.GetAddressBytes();
        if (ipBytes.Length != netBytes.Length) return false;

        var fullBytes = prefixLength / 8;
        for (int i = 0; i < fullBytes; i++)
        {
            if (ipBytes[i] != netBytes[i]) return false;
        }

        var remainingBits = prefixLength % 8;
        if (remainingBits == 0) return true;

        var mask = (byte)(0xFF << (8 - remainingBits));
        return (ipBytes[fullBytes] & mask) == (netBytes[fullBytes] & mask);
    }

    private static List<(IPAddress, int)> ParseCidrs(
        string[] cidrs,
        ILogger<WebhookIpWhitelistMiddleware> logger)
    {
        var result = new List<(IPAddress, int)>();
        foreach (var raw in cidrs)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var parts = raw.Split('/');
            if (parts.Length != 2 ||
                !IPAddress.TryParse(parts[0], out var addr) ||
                !int.TryParse(parts[1], out var prefix))
            {
                // Fail-fast в Configure не делаем — мы в middleware
                // ctor. Лог + skip: webhook просто не получит этот IP
                // в whitelist, что безопаснее чем падение API.
                logger.LogError(
                    "Невалидный CIDR в WebhookSecurity:AllowedCidrs: {Raw}", raw);
                continue;
            }
            result.Add((addr, prefix));
        }
        return result;
    }
}
