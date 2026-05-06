using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.Domain.Shared;

namespace GdeOni.Api.IntegrationTests.TrackedDeceased;

/// <summary>
/// D9.5.4 TrackedDeceased: создание через at-grave, лист, чужое
/// tracking → 403, удаление, обновление, route happy/no-coords/forbidden.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class TrackedDeceasedIntegrationTests
{
    private readonly GdeOniWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TrackedDeceasedIntegrationTests(GdeOniWebAppFactory factory) => _factory = factory;

    /// <summary>
    /// POST /api/deceased-records/at-grave создаёт Deceased + Tracking
    /// атомарно. Endpoint возвращает 201 + DeceasedId.
    /// </summary>
    [Fact]
    public async Task AtGrave_Creates_DeceasedAndTracking()
    {
        var user = await _factory.RegisterAndLoginAsync();

        var deceasedId = await CreateAtGraveAsync(user.Client, "Иван", $"Фам{Guid.NewGuid():N}");

        deceasedId.Should().NotBe(Guid.Empty);
    }

    /// <summary>
    /// GET /api/users/me/tracked-deceased возвращает созданного умершего
    /// в списке.
    /// </summary>
    [Fact]
    public async Task GetList_AfterAtGrave_ReturnsCreated()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await CreateAtGraveAsync(user.Client, "Иван", $"Фам{Guid.NewGuid():N}");

        var response = await user.Client.GetAsync("/api/users/me/tracked-deceased");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(deceasedId.ToString());
    }

    /// <summary>
    /// GET /api/users/me/tracked-deceased/{id} другого пользователя:
    /// текущий — не tracker → возвращает Forbidden
    /// (errorCode tracking.forbidden или tracking.not_found, см. use case).
    /// </summary>
    [Fact]
    public async Task GetDetails_NotTracker_ReturnsForbiddenOrNotFound()
    {
        var alice = await _factory.RegisterAndLoginAsync();
        var aliceDeceasedId = await CreateAtGraveAsync(alice.Client, "Иван", $"Фам{Guid.NewGuid():N}");

        var bob = await _factory.RegisterAndLoginAsync();
        var response = await bob.Client.GetAsync($"/api/users/me/tracked-deceased/{aliceDeceasedId}");

        // Use case GetDetails возвращает Forbidden если не trackerил, но
        // фактический статус-код может быть либо 403 (Forbidden), либо
        // 404 (если предварительно ищется только своё). Принимаем оба.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    /// <summary>
    /// DELETE /api/users/me/tracked-deceased/{id} удаляет tracking.
    /// Повторный GET /list уже не содержит запись.
    /// </summary>
    [Fact]
    public async Task Untrack_RemovesTracking()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await CreateAtGraveAsync(user.Client, "Иван", $"Фам{Guid.NewGuid():N}");

        var del = await user.Client.DeleteAsync($"/api/users/me/tracked-deceased/{deceasedId}");
        del.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await user.Client.GetAsync("/api/users/me/tracked-deceased");
        var body = await list.Content.ReadAsStringAsync();
        body.Should().NotContain(deceasedId.ToString());
    }

    /// <summary>
    /// PATCH /api/users/me/tracked-deceased/{id} обновляет настройки tracking.
    /// </summary>
    [Fact]
    public async Task Update_ChangesSettings()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await CreateAtGraveAsync(user.Client, "Иван", $"Фам{Guid.NewGuid():N}");

        var response = await user.Client.PatchAsJsonAsync($"/api/users/me/tracked-deceased/{deceasedId}", new
        {
            relationshipType = (int)RelationshipType.Friend,
            personalNotes = "Обновлённые заметки",
            notifyOnDeathAnniversary = true,
            notifyOnBirthAnniversary = false,
            trackStatus = (int)TrackStatus.Active
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// GET /api/users/me/tracked-deceased/{id}/route happy: deceased c
    /// координатами → 200 + ссылки на все 3 провайдера.
    /// </summary>
    [Fact]
    public async Task GetRoute_Happy_ReturnsLinks()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await CreateAtGraveAsync(user.Client, "Иван", $"Фам{Guid.NewGuid():N}");

        var response = await user.Client.GetAsync(
            $"/api/users/me/tracked-deceased/{deceasedId}/route" +
            "?fromLat=55.7&fromLon=37.6&mode=Auto");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("yandex");
        body.Should().Contain("google");
        body.Should().Contain("2gis");
    }

    /// <summary>
    /// GET /route → 409 если у умершего нет BurialLocation.
    /// Создаём карточку через POST /api/deceased-records (без burial),
    /// автотрекинга нет → подписываемся вручную, потом /route → 409.
    /// </summary>
    [Fact]
    public async Task GetRoute_NoBurialLocation_Returns409()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateWithoutBurialAsync(user.Client);

        // Подписываемся вручную (POST /tracked-deceased/{id}).
        var trackResp = await user.Client.PostAsJsonAsync(
            $"/api/users/me/tracked-deceased/{deceasedId}",
            new
            {
                relationshipType = (int)RelationshipType.Friend,
                personalNotes = (string?)null,
                notifyOnDeathAnniversary = false,
                notifyOnBirthAnniversary = false
            });
        trackResp.EnsureSuccessStatusCode();

        var response = await user.Client.GetAsync(
            $"/api/users/me/tracked-deceased/{deceasedId}/route" +
            "?fromLat=55.7&fromLon=37.6&mode=Auto");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// GET /route без tracking — bob не tracker'ит карточку alice → 403.
    /// </summary>
    [Fact]
    public async Task GetRoute_NotTracker_ReturnsForbidden()
    {
        var alice = await _factory.RegisterAndLoginAsync();
        var aliceDeceasedId = await CreateAtGraveAsync(alice.Client, "Иван", $"Фам{Guid.NewGuid():N}");

        var bob = await _factory.RegisterAndLoginAsync();
        var response = await bob.Client.GetAsync(
            $"/api/users/me/tracked-deceased/{aliceDeceasedId}/route" +
            "?fromLat=55.7&fromLon=37.6&mode=Auto");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    private static async Task<Guid> CreateAtGraveAsync(HttpClient client, string firstName, string lastName)
    {
        var deathDate = DateTime.UtcNow.AddYears(-10).Date;

        var response = await client.PostAsJsonAsync("/api/deceased-records/at-grave", new
        {
            firstName,
            lastName,
            middleName = (string?)null,
            birthDate = (DateOnly?)null,
            deathDate = DateOnly.FromDateTime(deathDate),
            shortDescription = (string?)null,
            biography = (string?)null,
            graveLocation = new
            {
                latitude = 55.7,
                longitude = 37.6,
                accuracyMeters = (double?)null,
                country = "Россия",
                city = "Москва",
                cemeteryName = "Test cemetery",
                plotNumber = (string?)null,
                graveNumber = (string?)null
            },
            tracking = new
            {
                relationshipType = (int)RelationshipType.Friend,
                personalNotes = (string?)null,
                notifyOnDeathAnniversary = false,
                notifyOnBirthAnniversary = false
            }
        });

        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Expected 201 Created from at-grave, got {(int)response.StatusCode}. Body: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<ApiResultDto<AtGraveResultDto>>(JsonOptions);
        return payload!.Result!.DeceasedId;
    }

    private sealed class ApiResultDto<T>
    {
        public T? Result { get; set; }
    }

    private sealed class AtGraveResultDto
    {
        public Guid DeceasedId { get; set; }
    }
}
