using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;
using GdeOni.Domain.Shared;

namespace GdeOni.Api.IntegrationTests.BurialLocation;

/// <summary>
/// D9.5.4 BurialLocation / Metadata: PUT from-gps автор → 200,
/// outsider → 403; DELETE metadata автор → 200.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class BurialLocationAndMetadataTests
{
    private readonly GdeOniWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BurialLocationAndMetadataTests(GdeOniWebAppFactory factory) => _factory = factory;

    /// <summary>
    /// PUT /burial-location/from-gps автором карточки → 200.
    /// </summary>
    [Fact]
    public async Task SetFromGps_Author_Returns200()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await CreateAtGraveAsync(user.Client);

        var response = await user.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/burial-location/from-gps",
            new { latitude = 50.0, longitude = 40.0, accuracyMeters = 5.0 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// PUT /burial-location/from-gps outsider'ом → 403.
    /// </summary>
    [Fact]
    public async Task SetFromGps_Outsider_Returns403()
    {
        var alice = await _factory.RegisterAndLoginAsync();
        var deceasedId = await CreateAtGraveAsync(alice.Client);

        var bob = await _factory.RegisterAndLoginAsync();
        var response = await bob.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/burial-location/from-gps",
            new { latitude = 50.0, longitude = 40.0, accuracyMeters = 5.0 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// PUT /metadata автором → 200, outsider → 403.
    /// </summary>
    [Fact]
    public async Task SetMetadata_AuthorAndOutsider()
    {
        var alice = await _factory.RegisterAndLoginAsync();
        var deceasedId = await CreateAtGraveAsync(alice.Client);

        var setAuthor = await alice.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/metadata",
            new
            {
                epitaph = "Покойся с миром",
                religion = (string?)null,
                source = (string?)null,
                additionalInfo = (string?)null
            });
        setAuthor.StatusCode.Should().Be(HttpStatusCode.OK);

        var bob = await _factory.RegisterAndLoginAsync();
        var setOutsider = await bob.Client.PutAsJsonAsync(
            $"/api/deceased-records/{deceasedId}/metadata",
            new
            {
                epitaph = "hacked",
                religion = (string?)null,
                source = (string?)null,
                additionalInfo = (string?)null
            });
        setOutsider.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// DELETE /burial-location → 409 если уже null. Создаём карточку
    /// без burial (через regular Create), затем DELETE → 409
    /// (deceased.burial_location.already_null или подобное).
    /// </summary>
    [Fact]
    public async Task DeleteBurialLocation_AlreadyNull_Returns409()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await TestSeed.CreateWithoutBurialAsync(user.Client);

        var response = await user.Client.DeleteAsync(
            $"/api/deceased-records/{deceasedId}/burial-location");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// DELETE /metadata автором → 200.
    /// </summary>
    [Fact]
    public async Task DeleteMetadata_Author_Returns200()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var deceasedId = await CreateAtGraveAsync(user.Client);

        var response = await user.Client.DeleteAsync(
            $"/api/deceased-records/{deceasedId}/metadata");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<Guid> CreateAtGraveAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/deceased-records/at-grave", new
        {
            firstName = "Иван",
            lastName = $"Фам{Guid.NewGuid():N}",
            middleName = (string?)null,
            birthDate = (DateOnly?)null,
            deathDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10)),
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
            throw new Xunit.Sdk.XunitException($"at-grave failed: {(int)response.StatusCode}. Body: {body}");
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
