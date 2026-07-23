using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.Application.Legal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    /// <summary>
    /// D19.9. Версии больше не хардкодим: они поднимаются при каждой
    /// редакции документа, и тест не должен падать из-за этого. Берём
    /// ожидаемое значение из тех же LegalOptions, что использует API
    /// (а startup-check гарантирует, что оно совпадает с текстом .md).
    /// </summary>
    private LegalOptions Legal =>
        _factory.Services.GetRequiredService<IOptions<LegalOptions>>().Value;

    [Fact]
    public async Task GetPrivacyPolicy_Anonymous_Returns200WithVersionAndBody()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/legal/privacy-policy");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        result.GetProperty("documentKey").GetString().Should().Be("privacy_policy");
        result.GetProperty("version").GetInt32().Should().Be(Legal.CurrentPrivacyPolicyVersion);
        result.GetProperty("url").GetString().Should().NotBeNullOrWhiteSpace();

        // D19.9: текст едет с бэка — клиент не хранит свою копию.
        var markdown = result.GetProperty("bodyMarkdown").GetString();
        markdown.Should().NotBeNullOrWhiteSpace();
        markdown.Should().Contain("Политика конфиденциальности");
        markdown.Should().Contain($"Редакция {Legal.CurrentPrivacyPolicyVersion}");
    }

    [Fact]
    public async Task GetTermsOfUse_Anonymous_Returns200WithVersionAndBody()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/legal/terms-of-use");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        result.GetProperty("documentKey").GetString().Should().Be("terms_of_use");
        result.GetProperty("version").GetInt32().Should().Be(Legal.CurrentTermsVersion);

        var markdown = result.GetProperty("bodyMarkdown").GetString();
        markdown.Should().NotBeNullOrWhiteSpace();
        markdown.Should().Contain("Пользовательское соглашение");
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
        // Register фиксирует те версии, что сейчас в LegalOptions.
        result.GetProperty("privacyPolicyVersion").GetInt32()
            .Should().Be(Legal.CurrentPrivacyPolicyVersion);
        result.GetProperty("termsVersion").GetInt32()
            .Should().Be(Legal.CurrentTermsVersion);
        result.GetProperty("hasOutdatedLegalAcceptance").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task AcceptLegal_CurrentVersions_Returns204()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.PostAsJsonAsync(
            "/api/users/me/accept-legal",
            new
            {
                privacyPolicyVersion = Legal.CurrentPrivacyPolicyVersion,
                termsVersion = Legal.CurrentTermsVersion,
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AcceptLegal_InvalidVersion_Returns400()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.PostAsJsonAsync(
            "/api/users/me/accept-legal",
            new { privacyPolicyVersion = 0, termsVersion = Legal.CurrentTermsVersion });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AcceptLegal_Anonymous_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/users/me/accept-legal",
            new
            {
                privacyPolicyVersion = Legal.CurrentPrivacyPolicyVersion,
                termsVersion = Legal.CurrentTermsVersion,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
