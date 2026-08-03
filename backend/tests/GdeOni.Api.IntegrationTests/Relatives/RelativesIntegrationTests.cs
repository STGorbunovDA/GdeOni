using System.Net;
using System.Net.Http.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.Domain.Shared;

namespace GdeOni.Api.IntegrationTests.Relatives;

/// <summary>
/// Функция «Родственники». Главная задача этих тестов — прогнать EF-запрос
/// GetRelativesForUser на реальном PostgreSQL (self-join tracked_deceased +
/// owned-колонки имени + теневой FK user_id), чтобы поймать ошибки
/// SQL-трансляции, которые unit-тесты не видят.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class RelativesIntegrationTests
{
    private readonly GdeOniWebAppFactory _factory;

    public RelativesIntegrationTests(GdeOniWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetRelatives_LoneUser_ReturnsEmptyOk()
    {
        var user = await _factory.RegisterAndLoginAsync();
        // Своя карточка есть, но других отслеживающих нет — список пуст, но
        // запрос выполняется полностью (валидируем трансляцию).
        await TestSeed.CreateAtGraveAsync(user.Client);

        var response = await user.Client.GetAsync("/api/relatives");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"items\"");
    }

    [Fact]
    public async Task GetRelatives_Anonymous_Unauthorized()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/relatives");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_LoneUser_ReturnsEmptyOk()
    {
        var user = await _factory.RegisterAndLoginAsync();
        // Прогоняем GetNewRelatives (join relative_discoveries + tracked +
        // owned-имя) на реальном PostgreSQL. Родственников/сообщений нет —
        // сводка пуста, но запрос выполняется полностью.
        await TestSeed.CreateAtGraveAsync(user.Client);

        var response = await user.Client.GetAsync("/api/relatives/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"newRelatives\"");
        body.Should().Contain("\"unreadConversations\"");
    }

    [Fact]
    public async Task MarkSeen_LoneUser_ReturnsOk()
    {
        var user = await _factory.RegisterAndLoginAsync();

        // ExecuteUpdate по relative_discoveries — no-op при пустой таблице,
        // но проверяем, что SQL-трансляция и эндпоинт отрабатывают.
        var response = await user.Client.PostAsync("/api/relatives/seen", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSummary_Anonymous_Unauthorized()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/relatives/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────── Жалобы (Фаза 5) ───────────────

    [Fact]
    public async Task Report_UnknownConversation_NotFound()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.PostAsJsonAsync(
            "/api/relatives/reports",
            new { conversationId = Guid.NewGuid(), reason = "спам" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Report_Anonymous_Unauthorized()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync(
            "/api/relatives/reports",
            new { conversationId = Guid.NewGuid(), reason = "спам" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminReports_NotAdmin_Forbidden()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var response = await user.Client.GetAsync("/api/admin/relative-reports?pendingOnly=true");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminReports_Admin_ReturnsEmptyOk()
    {
        // Прогоняем GetReports (owned-имя умершего + батч-джойны) на реальном
        // PostgreSQL под админом. Жалоб нет — список пуст, но запрос выполняется.
        var admin = await _factory.CreateAuthorizedUserWithRoleAsync(UserRole.Admin);

        var response = await admin.Client.GetAsync("/api/admin/relative-reports?pendingOnly=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"items\"");
    }
}
