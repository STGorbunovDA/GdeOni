using GdeOni.Api.IntegrationTests.Infrastructure;

namespace GdeOni.Api.IntegrationTests.Misc;

/// <summary>
/// D9.5.4 CORS: AddCustomCors настроена на http://localhost:5173.
/// Запрос с allowed origin → CORS-headers присутствуют; с не-allowed
/// origin → отсутствуют. ASP.NET Core CORS middleware не отдаёт 4xx —
/// отказ выражается в отсутствии Access-Control-Allow-Origin (это
/// заставляет браузер заблокировать ответ).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CorsTests
{
    private readonly HttpClient _client;

    public CorsTests(GdeOniWebAppFactory factory) => _client = factory.CreateClient();

    /// <summary>
    /// CORS preflight OPTIONS с allowed origin (http://localhost:5173)
    /// → response содержит Access-Control-Allow-Origin.
    /// </summary>
    [Fact]
    public async Task Preflight_AllowedOrigin_HasAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/users");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
    }

    /// <summary>
    /// CORS preflight с не-allowed origin → Access-Control-Allow-Origin
    /// отсутствует. Это и есть «отказ» по контракту CORS.
    /// </summary>
    [Fact]
    public async Task Preflight_DisallowedOrigin_NoAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/users");
        request.Headers.Add("Origin", "http://evil.example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
