using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;
using GdeOni.Infrastructure.Persistence.Repositories;

namespace GdeOni.Infrastructure.Tests.Persistence;

/// <summary>
/// Тесты <see cref="DeceasedRepository"/> на реальном Postgres
/// (Testcontainers). Покрывают фильтры пагинации, filtered Include
/// и unique-constraint на SearchKey.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class DeceasedRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public DeceasedRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    /// <summary>
    /// GetPaged.Search: ILike матчит по FirstName / LastName / MiddleName.
    /// Проверяем case-insensitive (Postgres ILIKE) и %substring% поведение.
    /// </summary>
    [Fact]
    public async Task GetPaged_SearchByName_FiltersILikeOnAllNameFields()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repo = new DeceasedRepository(dbContext);

        var user = TestData.SeedUser(dbContext);
        var ivanov = TestData.SeedDeceased(dbContext, user.Id, "Иван", "ИвановУникальный1");
        var petrov = TestData.SeedDeceased(dbContext, user.Id, "ПетрУникальный1", "Петров");
        var sidorov = TestData.SeedDeceased(dbContext, user.Id, "Сидор", "Сидоров", "ОсобоеОтчество1");
        await dbContext.SaveChangesAsync();

        var byLastName = await repo.GetPaged(
            new GetAllDeceasedQuery("ивановуникальный1", null, null, null, null, null, 1, 10),
            CancellationToken.None);
        byLastName.Items.Should().ContainSingle(x => x.Id == ivanov.Id);

        var byFirstName = await repo.GetPaged(
            new GetAllDeceasedQuery("ПетрУникальный1", null, null, null, null, null, 1, 10),
            CancellationToken.None);
        byFirstName.Items.Should().ContainSingle(x => x.Id == petrov.Id);

        var byMiddleName = await repo.GetPaged(
            new GetAllDeceasedQuery("ОсобоеОтчество1", null, null, null, null, null, 1, 10),
            CancellationToken.None);
        byMiddleName.Items.Should().ContainSingle(x => x.Id == sidorov.Id);
    }

    /// <summary>
    /// GetPaged.Country / City: ILike по BurialLocation.Country / City.
    /// </summary>
    [Fact]
    public async Task GetPaged_CountryAndCity_FilterByBurialLocation()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repo = new DeceasedRepository(dbContext);

        var user = TestData.SeedUser(dbContext);
        var ru = TestData.SeedDeceased(dbContext, user.Id, "Имя1", "ФамилияRu1",
            burialLocation: BurialLocation.Create(55, 37, country: "RussiaUniq1", city: "MoscowUniq1").Value);
        var de = TestData.SeedDeceased(dbContext, user.Id, "Имя2", "ФамилияDe1",
            burialLocation: BurialLocation.Create(52, 13, country: "GermanyUniq1", city: "BerlinUniq1").Value);
        await dbContext.SaveChangesAsync();

        var byCountry = await repo.GetPaged(
            new GetAllDeceasedQuery(null, "RussiaUniq1", null, null, null, null, 1, 10),
            CancellationToken.None);
        byCountry.Items.Should().ContainSingle(x => x.Id == ru.Id);

        var byCity = await repo.GetPaged(
            new GetAllDeceasedQuery(null, null, "BerlinUniq1", null, null, null, 1, 10),
            CancellationToken.None);
        byCity.Items.Should().ContainSingle(x => x.Id == de.Id);
    }

    /// <summary>
    /// GetPaged.IsVerified: фильтр по флагу верификации.
    /// </summary>
    [Fact]
    public async Task GetPaged_IsVerified_FiltersOnFlag()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repo = new DeceasedRepository(dbContext);

        var user = TestData.SeedUser(dbContext);
        var verified = TestData.SeedDeceased(dbContext, user.Id, "Имя1", "ФамилияV1");
        verified.Verify();
        var unverified = TestData.SeedDeceased(dbContext, user.Id, "Имя2", "ФамилияV2");
        await dbContext.SaveChangesAsync();

        var verifiedPage = await repo.GetPaged(
            new GetAllDeceasedQuery("ФамилияV", null, null, true, null, null, 1, 10),
            CancellationToken.None);
        verifiedPage.Items.Should().ContainSingle(x => x.Id == verified.Id);

        var unverifiedPage = await repo.GetPaged(
            new GetAllDeceasedQuery("ФамилияV", null, null, false, null, null, 1, 10),
            CancellationToken.None);
        unverifiedPage.Items.Should().ContainSingle(x => x.Id == unverified.Id);
    }

    /// <summary>
    /// GetPaged.CreatedFrom / CreatedTo: фильтр по диапазону дат.
    /// </summary>
    [Fact]
    public async Task GetPaged_CreatedFromAndTo_FilterRange()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repo = new DeceasedRepository(dbContext);

        var user = TestData.SeedUser(dbContext);
        var marker = $"ФамилияRange{Guid.NewGuid():N}";
        var d = TestData.SeedDeceased(dbContext, user.Id, "Имя1", marker);
        await dbContext.SaveChangesAsync();

        var inRange = await repo.GetPaged(
            new GetAllDeceasedQuery(
                marker, null, null, null,
                CreatedFrom: d.CreatedAtUtc.AddMinutes(-1),
                CreatedTo: d.CreatedAtUtc.AddMinutes(1),
                1, 10),
            CancellationToken.None);
        inRange.Items.Should().ContainSingle(x => x.Id == d.Id);

        var outOfRange = await repo.GetPaged(
            new GetAllDeceasedQuery(
                marker, null, null, null,
                CreatedFrom: d.CreatedAtUtc.AddMinutes(10),
                CreatedTo: d.CreatedAtUtc.AddMinutes(20),
                1, 10),
            CancellationToken.None);
        outOfRange.Items.Should().BeEmpty();
    }

    /// <summary>
    /// GetByIdWithMemories — Include(Memories) грузит коллекцию из БД.
    /// </summary>
    [Fact]
    public async Task GetByIdWithMemories_LoadsAllMemories()
    {
        Guid deceasedId;
        await using (var seedContext = _fixture.CreateDbContext())
        {
            var user = TestData.SeedUser(seedContext);
            var d = TestData.SeedDeceased(seedContext, user.Id, "Имя1", "ФамилияM1");
            d.AddMemory("Текст-1", user.Id);
            d.AddMemory("Текст-2", user.Id);
            await seedContext.SaveChangesAsync();
            deceasedId = d.Id;
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new DeceasedRepository(dbContext);

        var loaded = await repo.GetByIdWithMemories(deceasedId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Memories.Should().HaveCount(2);
    }

    /// <summary>
    /// GetByIdWithMemoryById: filtered Include грузит ровно одну memory.
    /// </summary>
    [Fact]
    public async Task GetByIdWithMemoryById_FilteredInclude_LoadsOnlyOne()
    {
        Guid deceasedId;
        Guid targetMemoryId;
        await using (var seedContext = _fixture.CreateDbContext())
        {
            var user = TestData.SeedUser(seedContext);
            var d = TestData.SeedDeceased(seedContext, user.Id, "Имя1", "ФамилияMM1");
            var m1 = d.AddMemory("Текст-1", user.Id).Value;
            d.AddMemory("Текст-2", user.Id);
            await seedContext.SaveChangesAsync();
            deceasedId = d.Id;
            targetMemoryId = m1.Id;
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new DeceasedRepository(dbContext);

        var loaded = await repo.GetByIdWithMemoryById(deceasedId, targetMemoryId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Memories.Should().HaveCount(1);
        loaded.Memories.Single().Id.Should().Be(targetMemoryId);
    }

    /// <summary>
    /// GetByIdWithMediaById: filtered Include грузит ровно одну media.
    /// </summary>
    [Fact]
    public async Task GetByIdWithMediaById_FilteredInclude_LoadsOnlyOne()
    {
        Guid deceasedId;
        Guid targetMediaId;
        await using (var seedContext = _fixture.CreateDbContext())
        {
            var user = TestData.SeedUser(seedContext);
            var d = TestData.SeedDeceased(seedContext, user.Id, "Имя1", "ФамилияMd1");
            var m1 = d.AddMedia(user.Id, MediaKind.DeceasedPhoto,
                "p1.jpg", "deceased-photos", $"k1/{Guid.NewGuid()}", "image/jpeg", 1000).Value;
            d.AddMedia(user.Id, MediaKind.DeceasedPhoto,
                "p2.jpg", "deceased-photos", $"k2/{Guid.NewGuid()}", "image/jpeg", 2000);
            await seedContext.SaveChangesAsync();
            deceasedId = d.Id;
            targetMediaId = m1.Id;
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new DeceasedRepository(dbContext);

        var loaded = await repo.GetByIdWithMediaById(deceasedId, targetMediaId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Media.Should().HaveCount(1);
        loaded.Media.Single().Id.Should().Be(targetMediaId);
    }

    /// <summary>
    /// ExistsBySearchKey: true для существующего, false для отсутствующего.
    /// </summary>
    [Fact]
    public async Task ExistsBySearchKey_TrueAndFalse()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repo = new DeceasedRepository(dbContext);

        var user = TestData.SeedUser(dbContext);
        var d = TestData.SeedDeceased(dbContext, user.Id, "УникИмя1", "УникФамилия1");
        await dbContext.SaveChangesAsync();

        (await repo.ExistsBySearchKey(d.SearchKey, CancellationToken.None)).Should().BeTrue();
        (await repo.ExistsBySearchKey("definitely|not|in|db|key", CancellationToken.None)).Should().BeFalse();
    }

    /// <summary>
    /// Save: вставка дубля SearchKey → UniqueConstraintException
    /// (имя индекса <see cref="DbConstraints.DeceasedSearchKey"/>).
    /// </summary>
    [Fact]
    public async Task Save_DuplicateSearchKey_ThrowsUniqueConstraintException()
    {
        var firstName = $"DuplИмя{Guid.NewGuid():N}";
        Guid userId;
        DateOnly deathDate;

        await using (var seedContext = _fixture.CreateDbContext())
        {
            var user = TestData.SeedUser(seedContext);
            userId = user.Id;
            // Фиксируем deathDate — нужно чтобы оба Deceased имели один SearchKey.
            deathDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-50));
            var d1 = Deceased.Create(firstName, "DuplФам1", null, null, deathDate, null, userId).Value;
            seedContext.DeceasedRecords.Add(d1);
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new DeceasedRepository(dbContext);

        var d2 = Deceased.Create(firstName, "DuplФам1", null, null, deathDate, null, userId).Value;
        dbContext.DeceasedRecords.Add(d2);

        var act = () => repo.Save(CancellationToken.None);
        var ex = await act.Should().ThrowAsync<UniqueConstraintException>();
        ex.Which.ConstraintName.Should().Be(DbConstraints.DeceasedSearchKey);
    }
}
