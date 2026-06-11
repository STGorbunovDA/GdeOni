using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using GdeOni.Infrastructure.Persistence.Repositories;

namespace GdeOni.Infrastructure.Tests.Persistence;

/// <summary>
/// Тесты <see cref="UserRepository"/> на реальном Postgres
/// (Testcontainers). Покрывают нормализацию email/userName,
/// IsActivelyTracking по статусам, GetByIdWithTrackingCount и
/// unique-constraint на email.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class UserRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public UserRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    /// <summary>
    /// GetByEmail: User.Register нормализует email в lowercase,
    /// поэтому даже если запросить с UPPERCASE — найдётся.
    /// </summary>
    [Fact]
    public async Task GetByEmail_ReturnsUser_RegardlessOfCase()
    {
        var email = $"User-{Guid.NewGuid():N}@Example.COM";
        await using (var seedContext = _fixture.CreateDbContext())
        {
            var user = User.Register(email, "hash").Value;
            await seedContext.Users.AddAsync(user);
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new UserRepository(dbContext);

        var lower = await repo.GetByEmail(email.ToLowerInvariant(), CancellationToken.None);
        var upper = await repo.GetByEmail(email.ToUpperInvariant(), CancellationToken.None);

        lower.Should().NotBeNull();
        lower!.Email.Should().Be(email.ToLowerInvariant());
        upper.Should().NotBeNull();
        upper!.Id.Should().Be(lower.Id);
    }

    /// <summary>
    /// ExistsByEmail / ExistsByUserName: case-insensitive matching через
    /// нормализованные поля Email и UserNameNormalized.
    /// </summary>
    [Fact]
    public async Task ExistsByEmailAndUserName_AreCaseInsensitive()
    {
        var email = $"caseu-{Guid.NewGuid():N}@example.com";
        var userName = $"UserCase{Guid.NewGuid():N}";
        await using (var seedContext = _fixture.CreateDbContext())
        {
            var user = User.Register(email, "hash", userName: userName).Value;
            await seedContext.Users.AddAsync(user);
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new UserRepository(dbContext);

        (await repo.ExistsByEmail(email.ToUpperInvariant(), CancellationToken.None)).Should().BeTrue();
        (await repo.ExistsByUserName(userName.ToUpperInvariant(), CancellationToken.None)).Should().BeTrue();
        (await repo.ExistsByEmail($"missing-{Guid.NewGuid():N}@example.com", CancellationToken.None)).Should().BeFalse();
    }

    /// <summary>
    /// IsActivelyTracking: true для Active/Muted, false для Archived
    /// и при отсутствии записи. Логика: != Archived.
    /// </summary>
    [Fact]
    public async Task IsActivelyTracking_StatusBased()
    {
        Guid activeUserId, mutedUserId, archivedUserId, deceasedId;

        await using (var seedContext = _fixture.CreateDbContext())
        {
            var creator = TestData.SeedUser(seedContext);
            var d = TestData.SeedDeceased(seedContext, creator.Id, "Имя1", $"Фам{Guid.NewGuid():N}");

            var active = User.Register($"a-{Guid.NewGuid():N}@example.com", "hash").Value;
            active.TrackDeceased(d.Id, RelationshipType.Friend);
            activeUserId = active.Id;

            var muted = User.Register($"m-{Guid.NewGuid():N}@example.com", "hash").Value;
            muted.TrackDeceased(d.Id, RelationshipType.Friend);
            muted.ChangeTrackingStatus(d.Id, TrackStatus.Muted);
            mutedUserId = muted.Id;

            var archived = User.Register($"r-{Guid.NewGuid():N}@example.com", "hash").Value;
            archived.TrackDeceased(d.Id, RelationshipType.Friend);
            archived.ChangeTrackingStatus(d.Id, TrackStatus.Archived);
            archivedUserId = archived.Id;

            seedContext.Users.AddRange(active, muted, archived);
            await seedContext.SaveChangesAsync();
            deceasedId = d.Id;
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new UserRepository(dbContext);

        (await repo.IsActivelyTracking(activeUserId, deceasedId, CancellationToken.None)).Should().BeTrue();
        (await repo.IsActivelyTracking(mutedUserId, deceasedId, CancellationToken.None)).Should().BeTrue();
        (await repo.IsActivelyTracking(archivedUserId, deceasedId, CancellationToken.None)).Should().BeFalse();
        (await repo.IsActivelyTracking(Guid.NewGuid(), deceasedId, CancellationToken.None)).Should().BeFalse();
    }

    /// <summary>
    /// GetByIdWithTrackingCount возвращает (User, COUNT) — COUNT
    /// считается через subquery в SQL, без подгрузки коллекции.
    /// </summary>
    [Fact]
    public async Task GetByIdWithTrackingCount_ReturnsCount()
    {
        Guid userId;
        await using (var seedContext = _fixture.CreateDbContext())
        {
            var creator = TestData.SeedUser(seedContext);
            var d1 = TestData.SeedDeceased(seedContext, creator.Id, "Имя1", $"Фам{Guid.NewGuid():N}");
            var d2 = TestData.SeedDeceased(seedContext, creator.Id, "Имя2", $"Фам{Guid.NewGuid():N}");
            var d3 = TestData.SeedDeceased(seedContext, creator.Id, "Имя3", $"Фам{Guid.NewGuid():N}");

            var user = User.Register($"tc-{Guid.NewGuid():N}@example.com", "hash").Value;
            user.TrackDeceased(d1.Id, RelationshipType.Friend);
            user.TrackDeceased(d2.Id, RelationshipType.Friend);
            user.TrackDeceased(d3.Id, RelationshipType.Friend);
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
            userId = user.Id;
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new UserRepository(dbContext);

        var row = await repo.GetByIdWithTrackingCount(userId, CancellationToken.None);

        row.Should().NotBeNull();
        row!.Value.User.Id.Should().Be(userId);
        row.Value.TrackingCount.Should().Be(3);
    }

    /// <summary>
    /// GetByIdWithTrackingByDeceasedId: filtered Include грузит ровно
    /// одно tracking. Остальные не материализуются.
    /// </summary>
    [Fact]
    public async Task GetByIdWithTrackingByDeceasedId_FilteredInclude_LoadsOnlyOne()
    {
        Guid userId;
        Guid targetDeceasedId;
        await using (var seedContext = _fixture.CreateDbContext())
        {
            var creator = TestData.SeedUser(seedContext);
            var dTarget = TestData.SeedDeceased(seedContext, creator.Id, "ИмяT", $"Фам{Guid.NewGuid():N}");
            var dOther1 = TestData.SeedDeceased(seedContext, creator.Id, "Имя1", $"Фам{Guid.NewGuid():N}");
            var dOther2 = TestData.SeedDeceased(seedContext, creator.Id, "Имя2", $"Фам{Guid.NewGuid():N}");

            var user = User.Register($"flt-{Guid.NewGuid():N}@example.com", "hash").Value;
            user.TrackDeceased(dTarget.Id, RelationshipType.Friend);
            user.TrackDeceased(dOther1.Id, RelationshipType.Friend);
            user.TrackDeceased(dOther2.Id, RelationshipType.Friend);
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
            userId = user.Id;
            targetDeceasedId = dTarget.Id;
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new UserRepository(dbContext);

        var loaded = await repo.GetByIdWithTrackingByDeceasedId(userId, targetDeceasedId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.TrackedDeceasedItems.Should().HaveCount(1);
        loaded.TrackedDeceasedItems.Single().DeceasedId.Should().Be(targetDeceasedId);
    }

    /// <summary>
    /// GetMyTrackedDeceasedPaged: JOIN по DeceasedId возвращает пары
    /// (Tracking, Deceased) только для запрошенного userId.
    /// </summary>
    [Fact]
    public async Task GetMyTrackedDeceasedPaged_JoinsDeceasedAndOnlyOwnRows()
    {
        Guid userId;
        Guid otherUserId;
        await using (var seedContext = _fixture.CreateDbContext())
        {
            var creator = TestData.SeedUser(seedContext);
            var d1 = TestData.SeedDeceased(seedContext, creator.Id, "Имя1", $"Фам{Guid.NewGuid():N}");
            var d2 = TestData.SeedDeceased(seedContext, creator.Id, "Имя2", $"Фам{Guid.NewGuid():N}");

            var user = User.Register($"jp-{Guid.NewGuid():N}@example.com", "hash").Value;
            user.TrackDeceased(d1.Id, RelationshipType.Friend);
            user.TrackDeceased(d2.Id, RelationshipType.Friend);
            userId = user.Id;

            var other = User.Register($"jp2-{Guid.NewGuid():N}@example.com", "hash").Value;
            other.TrackDeceased(d1.Id, RelationshipType.Friend);
            otherUserId = other.Id;

            seedContext.Users.AddRange(user, other);
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new UserRepository(dbContext);

        var page = await repo.GetMyTrackedDeceasedPaged(userId, 1, 10, CancellationToken.None);

        page.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(x => x.Tracking.DeceasedId == x.Deceased.Id);

        var otherPage = await repo.GetMyTrackedDeceasedPaged(otherUserId, 1, 10, CancellationToken.None);
        otherPage.Items.Should().HaveCount(1);
    }

    /// <summary>
    /// Save: дубль email → UniqueConstraintException c constraint
    /// = ux_users_email.
    /// </summary>
    [Fact]
    public async Task Save_DuplicateEmail_ThrowsUniqueConstraintException()
    {
        var email = $"dupl-{Guid.NewGuid():N}@example.com";

        await using (var seedContext = _fixture.CreateDbContext())
        {
            var first = User.Register(email, "hash").Value;
            await seedContext.Users.AddAsync(first);
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = _fixture.CreateDbContext();
        var repo = new UserRepository(dbContext);

        var second = User.Register(email, "hash").Value;
        await dbContext.Users.AddAsync(second);

        var act = () => repo.Save(CancellationToken.None);
        var ex = await act.Should().ThrowAsync<UniqueConstraintException>();
        ex.Which.ConstraintName.Should().Be(DbConstraints.UxUsersEmail);
    }
}
