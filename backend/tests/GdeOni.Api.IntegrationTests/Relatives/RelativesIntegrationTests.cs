using System.Net;
using GdeOni.Api.IntegrationTests.Infrastructure;

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
}
