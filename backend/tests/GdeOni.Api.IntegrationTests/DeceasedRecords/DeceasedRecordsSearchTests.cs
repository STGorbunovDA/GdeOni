using System.Net;
using System.Net.Http.Json;
using GdeOni.Api.IntegrationTests.Infrastructure;

namespace GdeOni.Api.IntegrationTests.DeceasedRecords;

/// <summary>
/// D15: GET /api/deceased-records открыт всем авторизованным —
/// нужно для E16 (поиск перед добавлением). Раньше был
/// [Authorize(Roles = "SuperAdmin,Admin")] и возвращал 403
/// обычному юзеру; этот тест ловит регрессию, если кто-то
/// случайно вернёт admin-only на эндпоинте.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class DeceasedRecordsSearchTests
{
    private readonly GdeOniWebAppFactory _factory;

    public DeceasedRecordsSearchTests(GdeOniWebAppFactory factory) => _factory = factory;

    /// <summary>
    /// GET /api/deceased-records обычный юзер → 200 + paged response.
    /// </summary>
    [Fact]
    public async Task GetAll_AnyAuthenticated_Returns200()
    {
        var user = await _factory.RegisterAndLoginAsync();
        // lastName=null → TestSeed подставит Guid-based уникальное
        // значение. Иначе при повторных прогонах sln по SearchKey
        // конфликт (deceased.already.exists).
        await TestSeed.CreateAtGraveAsync(user.Client);

        var response = await user.Client.GetAsync("/api/deceased-records?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// GET /api/deceased-records без авторизации → 401, поведение
    /// [Authorize] без ролей.
    /// </summary>
    [Fact]
    public async Task GetAll_Anonymous_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/deceased-records?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// GET /api/deceased-records?search=Кириллица — повторяет mobile
    /// сценарий поиска (юзер ввёл фамилию по-русски в DeceasedSearchPage).
    /// </summary>
    [Fact]
    public async Task GetAll_FiltersBySearch_Cyrillic()
    {
        var user = await _factory.RegisterAndLoginAsync();
        // Уникальный кириллический lastName — не должен ломаться по
        // нестабильности (на каждый прогон новая GUID-часть в начале).
        var marker = $"Кириллический{Guid.NewGuid():N}";
        await TestSeed.CreateAtGraveAsync(user.Client, lastName: marker);

        var response = await user.Client.GetAsync(
            $"/api/deceased-records?search={Uri.EscapeDataString(marker)}&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(marker);
    }

    /// <summary>
    /// E17.4: фильтр по дате смерти точно совпадает с DateOnly в БД.
    /// Создаём две карточки с одинаковой фамилией но разной DeathDate,
    /// проверяем что deathDate=... возвращает только одну.
    /// Это тест регрессии формата DateOnly через query string (mobile
    /// должен передавать "yyyy-MM-dd"; локальный "dd.MM.yyyy" не парсится).
    /// </summary>
    [Fact]
    public async Task GetAll_FiltersByDeathDate()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var marker = $"DeathDateMarker{Guid.NewGuid():N}";

        // Карточка 1 с deathDate=2020-06-15.
        var deathDate1 = new DateOnly(2020, 6, 15);
        await CreateAtGraveWithDeathDateAsync(user.Client, marker, deathDate1, cemetery: "A");

        // Карточка 2 с deathDate=2021-07-20.
        var deathDate2 = new DateOnly(2021, 7, 20);
        await CreateAtGraveWithDeathDateAsync(user.Client, marker, deathDate2, cemetery: "B");

        // Поиск только по фамилии — обе.
        var bothResp = await user.Client.GetAsync(
            $"/api/deceased-records?search={marker}&page=1&pageSize=10");
        bothResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var bothBody = await bothResp.Content.ReadAsStringAsync();
        bothBody.Should().Contain("2020-06-15");
        bothBody.Should().Contain("2021-07-20");

        // Поиск с deathDate=2020-06-15 — только первая.
        var filteredResp = await user.Client.GetAsync(
            $"/api/deceased-records?search={marker}&deathDate=2020-06-15&page=1&pageSize=10");
        filteredResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var filteredBody = await filteredResp.Content.ReadAsStringAsync();
        filteredBody.Should().Contain("2020-06-15");
        filteredBody.Should().NotContain("2021-07-20");
    }

    /// <summary>
    /// E17.4: фильтр по дате рождения — то же самое.
    /// </summary>
    [Fact]
    public async Task GetAll_FiltersByBirthDate()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var marker = $"BirthDateMarker{Guid.NewGuid():N}";

        var birthDate1 = new DateOnly(1950, 3, 10);
        await CreateAtGraveWithDeathDateAsync(
            user.Client, marker, new DateOnly(2020, 6, 15),
            birthDate: birthDate1, cemetery: "A");

        var birthDate2 = new DateOnly(1960, 4, 22);
        await CreateAtGraveWithDeathDateAsync(
            user.Client, marker, new DateOnly(2020, 6, 15),
            birthDate: birthDate2, cemetery: "B");

        var filteredResp = await user.Client.GetAsync(
            $"/api/deceased-records?search={marker}&birthDate=1950-03-10&page=1&pageSize=10");
        filteredResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await filteredResp.Content.ReadAsStringAsync();
        body.Should().Contain("1950-03-10");
        body.Should().NotContain("1960-04-22");
    }

    /// <summary>
    /// Ad-hoc создание карточки с произвольной BirthDate/DeathDate/cemetery.
    /// TestSeed.CreateAtGraveAsync не поддерживает кастомные даты.
    /// </summary>
    private static async Task CreateAtGraveWithDeathDateAsync(
        HttpClient client,
        string lastName,
        DateOnly deathDate,
        DateOnly? birthDate = null,
        string? cemetery = null,
        string firstName = "Иван")
    {
        var response = await client.PostAsJsonAsync("/api/deceased-records/at-grave", new
        {
            firstName,
            lastName,
            middleName = (string?)null,
            birthDate,
            deathDate,
            shortDescription = (string?)null,
            biography = (string?)null,
            graveLocation = new
            {
                latitude = 55.7,
                longitude = 37.6,
                accuracyMeters = (double?)null,
                country = "Россия",
                city = "Москва",
                cemeteryName = cemetery ?? "Test cemetery",
                plotNumber = (string?)null,
                graveNumber = (string?)null
            },
            tracking = new
            {
                relationshipType = "Friend",
                personalNotes = (string?)null,
                notifyOnDeathAnniversary = false,
                notifyOnBirthAnniversary = false
            }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            because: await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// E17.5: точечный поиск по lastName + firstName + middleName.
    /// Параметры AND'ятся — указали firstName=Иван + lastName=УникМаркер
    /// → нашли только этого Ивана, не других.
    /// </summary>
    [Fact]
    public async Task GetAll_FiltersByNameFields()
    {
        var user = await _factory.RegisterAndLoginAsync();
        var lastNameMarker = $"ФамилияМаркер{Guid.NewGuid():N}";

        // Создаём двух с одинаковой фамилией но разными именами.
        await CreateAtGraveWithDeathDateAsync(
            user.Client, lastNameMarker, new DateOnly(2020, 1, 1),
            cemetery: "Cem1", firstName: "Иван");
        await CreateAtGraveWithDeathDateAsync(
            user.Client, lastNameMarker, new DateOnly(2020, 1, 1),
            cemetery: "Cem2", firstName: "Пётр");

        // lastName=ФамилияМаркер → двое.
        var bothResp = await user.Client.GetAsync(
            $"/api/deceased-records?lastName={lastNameMarker}&page=1&pageSize=10");
        bothResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var bothBody = await bothResp.Content.ReadAsStringAsync();
        bothBody.Should().Contain("Иван");
        bothBody.Should().Contain("Пётр");

        // lastName=ФамилияМаркер + firstName=Иван → только Иван.
        var ivanResp = await user.Client.GetAsync(
            $"/api/deceased-records?lastName={lastNameMarker}&firstName=Иван&page=1&pageSize=10");
        ivanResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var ivanBody = await ivanResp.Content.ReadAsStringAsync();
        ivanBody.Should().Contain("Иван");
        ivanBody.Should().NotContain("Пётр");
    }

    /// <summary>
    /// GET /api/deceased-records?search=X находит совпадения по
    /// first/last/middle name (ILike substring).
    /// </summary>
    [Fact]
    public async Task GetAll_FiltersBySearch()
    {
        var user = await _factory.RegisterAndLoginAsync();
        // Уникальный маркер в lastName — иначе при повторном прогоне
        // sln conflict по SearchKey (deceased.already.exists).
        var marker = $"Маркер{Guid.NewGuid():N}";
        await TestSeed.CreateAtGraveAsync(user.Client, lastName: marker);

        var response = await user.Client.GetAsync(
            $"/api/deceased-records?search={marker}&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Smoke-проверка: тело пришло и в нём есть наш маркер.
        // В жёсткий envelope не парсим — GetAllDeceasedItemResponse
        // живёт в Application-слое, у IntegrationTests на него нет
        // PackageReference; для регрессионного теста достаточно
        // string-substring.
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(marker);
    }
}
