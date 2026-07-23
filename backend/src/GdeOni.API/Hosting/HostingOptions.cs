namespace GdeOni.API.Hosting;

/// <summary>
/// Опции хостинг-окружения. Используется для конфигурации
/// ForwardedHeadersMiddleware, когда API стоит за reverse-proxy
/// (nginx, ALB, Cloudflare и т.п.).
///
/// Если KnownProxies и KnownNetworks пусты — ForwardedHeaders не
/// подключается, и Connection.RemoteIpAddress возвращает peer-IP
/// как до D7.38. Прокси по умолчанию (loopback) ASP.NET Core
/// доверяет всегда, отдельной настройки не требуют.
///
/// Whitelist важен для безопасности: без него любой клиент
/// сможет подделать X-Forwarded-For и прикинуться чужим IP.
/// </summary>
public sealed class HostingOptions
{
    /// <summary>Имя секции в appsettings, к которой биндятся опции.</summary>
    public const string SectionName = "Hosting";

    /// <summary>
    /// Список IP-адресов прокси, которым доверяем заголовок
    /// X-Forwarded-For. Например: ["10.0.0.5", "172.20.0.1"].
    /// </summary>
    public string[]? KnownProxies { get; set; }

    /// <summary>
    /// Список доверенных сетей в CIDR-формате. Удобно для k8s/docker,
    /// где IP прокси может меняться. Например: ["10.0.0.0/8", "172.16.0.0/12"].
    /// </summary>
    public string[]? KnownNetworks { get; set; }
}
