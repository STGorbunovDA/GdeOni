using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;

namespace GdeOni.Api.IntegrationTests.Legal;

/// <summary>
/// D19. Тесты эндпоинтов Privacy / Terms / Accept Legal.
///
/// Покрываем:
///  - GET /api/legal/privacy-policy (AllowAnonymous, дефолтные опции);
///  - GET /api/legal/terms-of-use (AllowAnonymous);
///  - POST /api/users/me/accept-legal (BasicAuthenticated, current/outdated/invalid);
///  - регистрация без согласия → 400 + legal.privacy_policy.not_accepted;
///  - GET /api/users/me содержит PrivacyPolicyVersion / TermsVersion
///    после регистрации, HasOutdatedLegalAcceptance=false на свежем юзере.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class LegalIntegrationTests
{
    private readonly GdeOniWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LegalIntegrationTests(GdeOniWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetPrivacyPolicy_Anonymous_Returns200WithDefaults()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/legal/privacy-policy");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        result.GetProperty("documentKey").GetString().Should().Be("privacy_policy");
        result.GetProperty("version").GetInt32().Should().Be(1);
        result.GetProperty("url").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetTermsOfUse_Anonymous_Returns200WithDefaults()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/legal/terms-of-use");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        result.GetProperty("documentKey").GetString().Should().Be("terms_of_use");
        result.GetProperty("version").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Register_WithoutAcceptanceFlags_Returns400WithLegalErrors()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            email = $"int-{Guid.NewGuid():N}@example.com",
            password = "Password123!",
            // privacyPolicyAccepted/termsAccepted намеренно не передаём → false по умолчанию.
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var errors = doc.RootElement.GetProperty("errors");
        var codes = errors.EnumerateArray()
            .Select(e => e.GetProperty("errorCode").GetString())
            .ToList();
        codes.Should().Contain("legal.privacy_policy.not_accepted");
        codes.Should().Contain("legal.terms.not_accepted");
    }

    [Fact]
    public async Task GetMe_AfterRegister_ReturnsCurrentLegalVersions()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        // Дефолтные LegalOptions = v1; Register фиксирует те же v1.
        result.GetProperty("privacyPolicyVersion").GetInt32().Should().Be(1);
        result.GetProperty("termsVersion").GetInt32().Should().Be(1);
        result.GetProperty("hasOutdatedLegalAcceptance").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task AcceptLegal_CurrentVersions_Returns204()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.PostAsJsonAsync(
            "/api/users/me/accept-legal",
            new { privacyPolicyVersion = 1, termsVersion = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AcceptLegal_InvalidVersion_Returns400()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.PostAsJsonAsync(
            "/api/users/me/accept-legal",
            new { privacyPolicyVersion = 0, termsVersion = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AcceptLegal_Anonymous_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/users/me/accept-legal",
            new { privacyPolicyVersion = 1, termsVersion = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
