using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;

namespace GdeOni.API.Hosting;

/// <summary>
/// Регистрация ForwardedHeadersMiddleware для работы за reverse-proxy.
/// </summary>
public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Подключает ForwardedHeadersMiddleware, если в конфиге задан
    /// хотя бы один доверенный прокси или сеть. Без конфига middleware
    /// не подключается — поведение Connection.RemoteIpAddress остаётся
    /// прежним (peer-IP).
    ///
    /// Должно вызываться раньше остального middleware: схему/IP читают
    /// CORS, аутентификация, логи.
    /// </summary>
    public static IApplicationBuilder UseForwardedHeadersIfConfigured(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        var hosting = configuration
            .GetSection(HostingOptions.SectionName)
            .Get<HostingOptions>();

        var hasProxies = hosting?.KnownProxies is { Length: > 0 };
        var hasNetworks = hosting?.KnownNetworks is { Length: > 0 };

        if (!hasProxies && !hasNetworks)
            return app;

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        if (hasProxies)
        {
            foreach (var raw in hosting!.KnownProxies!)
            {
                if (IPAddress.TryParse(raw, out var ip))
                    options.KnownProxies.Add(ip);
            }
        }

        if (hasNetworks)
        {
            foreach (var cidr in hosting!.KnownNetworks!)
            {
                var parts = cidr.Split('/');
                if (parts.Length == 2
                    && IPAddress.TryParse(parts[0], out var prefix)
                    && int.TryParse(parts[1], out var prefixLength)
                    && prefixLength is >= 0 and <= 128)
                {
                    options.KnownNetworks.Add(
                        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
                }
            }
        }

        app.UseForwardedHeaders(options);
        return app;
    }
}
