using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetById.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Validation;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.Model;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты Deceased query use case'ов: GetAll (открыт всем для поиска,
/// D15), GetById, GetDistance, GetAgeAtDeath, HasMemories.
/// </summary>
public sealed class DeceasedQueriesTests
{
    private static readonly Guid CardAuthorId = Guid.NewGuid();

    /// <summary>
    /// GetAll любой авторизованный → success + GetPaged вызван.
    /// D15: GetAll открыт всем для функции поиска перед добавлением
    /// (E16 на mobile). Ранее был admin-only.
    /// </summary>
    [Fact]
    public async Task GetAll_AnyAuthenticated_ReturnsPaged()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var fileStorage = new Mock<IFileStorage>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo
            .Setup(x => x.GetPaged(It.IsAny<GetAllDeceasedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Deceased>(), 0));

        var useCase = new GetAllDeceasedUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetAllDeceasedQuery>(
                new GetAllDeceasedQueryValidator(TimeProvider.System)));

        var result = await useCase.Execute(
            new GetAllDeceasedQuery("Иван", null, null, null, null, null, null, null, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceasedRepo.Verify(
            x => x.GetPaged(It.IsAny<GetAllDeceasedQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// GetAll admin → success + GetPaged вызван.
    /// </summary>
    [Fact]
    public async Task GetAll_Admin_ReturnsPaged()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var fileStorage = new Mock<IFileStorage>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo
            .Setup(x => x.GetPaged(It.IsAny<GetAllDeceasedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Deceased>(), 0));

        var useCase = new GetAllDeceasedUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetAllDeceasedQuery>(
                new GetAllDeceasedQueryValidator(TimeProvider.System)));

        var result = await useCase.Execute(
            new GetAllDeceasedQuery(null, null, null, null, null, null, null, null, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        deceasedRepo.Verify(
            x => x.GetPaged(It.IsAny<GetAllDeceasedQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// GetById admin → CanSeeAllMemories=true.
    /// </summary>
    [Fact]
    public async Task GetById_Admin_CanSeeAllMemories()
    {
        var deceased = MakeDeceased();

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo
            .Setup(x => x.GetByIdWithMemoriesReadOnly(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var fileStorage = new Mock<IFileStorage>();
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetDisplayNamesByIds(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        var useCase = new GetDeceasedByIdUseCase(
            deceasedRepo.Object, userRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetDeceasedByIdQuery, GetDeceasedByIdQueryValidator>());

        var result = await useCase.Execute(
            new GetDeceasedByIdQuery(deceased.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanSeeAllMemories.Should().BeTrue();
    }

    /// <summary>
    /// GetById outsider → CanSeeAllMemories=true.
    /// D14: модерация воспоминаний отключена — фильтрация по
    /// canSeeAllMemories снята, все воспоминания видны всем.
    /// Параметр в Result оставлен для совместимости с mapper'ом.
    /// </summary>
    [Fact]
    public async Task GetById_Outsider_CanSeeAllMemories()
    {
        var deceased = MakeDeceased();

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo
            .Setup(x => x.GetByIdWithMemoriesReadOnly(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var fileStorage = new Mock<IFileStorage>();
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetDisplayNamesByIds(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        var useCase = new GetDeceasedByIdUseCase(
            deceasedRepo.Object, userRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetDeceasedByIdQuery, GetDeceasedByIdQueryValidator>());

        var result = await useCase.Execute(
            new GetDeceasedByIdQuery(deceased.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanSeeAllMemories.Should().BeTrue();
    }

    /// <summary>
    /// GetDistance happy: Haversine от user-coord до могилы.
    /// Москва (55.75, 37.62) ↔ С-Пб (59.94, 30.31) ≈ 635 км.
    /// </summary>
    [Fact]
    public async Task GetDistance_Happy_ReturnsHaversineDistance()
    {
        var burial = BurialLocation.Create(55.75, 37.62, country: "Россия", city: "Москва").Value;
        var deceased = Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            burial, CardAuthorId).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo
            .Setup(x => x.GetByIdReadOnly(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new GetDistanceUseCase(
            deceasedRepo.Object,
            TestExecutor.With<GetDistanceQuery, GetDistanceQueryValidator>());

        var result = await useCase.Execute(
            new GetDistanceQuery(deceased.Id, 59.94, 30.31),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // ~635 км; погрешность ±10 км достаточно для проверки формулы.
        result.Value.DistanceKm.Should().BeApproximately(635, 10);
    }

    /// <summary>
    /// GetDistance: BurialLocation == null → BurialLocationNotSet.
    /// </summary>
    [Fact]
    public async Task GetDistance_NoBurialLocation_ReturnsNotSet()
    {
        var deceased = MakeDeceased();

        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo
            .Setup(x => x.GetByIdReadOnly(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new GetDistanceUseCase(
            deceasedRepo.Object,
            TestExecutor.With<GetDistanceQuery, GetDistanceQueryValidator>());

        var result = await useCase.Execute(
            new GetDistanceQuery(deceased.Id, 0, 0),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.burial_location.not_set");
    }

    /// <summary>
    /// GetAgeAtDeath без BirthDate → AgeAtDeath = null.
    /// </summary>
    [Fact]
    public async Task GetAgeAtDeath_NoBirthDate_ReturnsNull()
    {
        var deceased = MakeDeceased(); // birthDate=null.

        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo
            .Setup(x => x.GetByIdReadOnly(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new GetAgeAtDeathUseCase(
            deceasedRepo.Object,
            TestExecutor.With<GetAgeAtDeathQuery, GetAgeAtDeathQueryValidator>());

        var result = await useCase.Execute(
            new GetAgeAtDeathQuery(deceased.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AgeAtDeath.Should().BeNull();
    }

    /// <summary>
    /// GetAgeAtDeath happy: с BirthDate возвращает количество полных лет.
    /// </summary>
    [Fact]
    public async Task GetAgeAtDeath_HasBirthDate_ReturnsAge()
    {
        var deceased = Deceased.Create(
            "Иван", "Иванов", null,
            new DateOnly(1950, 6, 1),
            new DateOnly(2010, 6, 1),
            null, CardAuthorId).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo
            .Setup(x => x.GetByIdReadOnly(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new GetAgeAtDeathUseCase(
            deceasedRepo.Object,
            TestExecutor.With<GetAgeAtDeathQuery, GetAgeAtDeathQueryValidator>());

        var result = await useCase.Execute(
            new GetAgeAtDeathQuery(deceased.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AgeAtDeath.Should().Be(60);
    }

    /// <summary>
    /// HasMemories true / false.
    /// </summary>
    [Fact]
    public async Task HasMemories_TrueAndFalse()
    {
        var deceased = MakeDeceased();
        var memoryResult = deceased.AddMemory("Текст", CardAuthorId);
        // После D11.4.7 HasMemories считает только Approved — добавление
        // ещё не делает воспоминание видимым.
        deceased.ApproveMemory(memoryResult.Value.Id);

        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo
            .Setup(x => x.GetByIdWithMemories(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new HasMemoriesUseCase(
            deceasedRepo.Object,
            TestExecutor.With<HasMemoriesQuery, HasMemoriesQueryValidator>());

        var withMemories = await useCase.Execute(
            new HasMemoriesQuery(deceased.Id), CancellationToken.None);
        withMemories.IsSuccess.Should().BeTrue();
        withMemories.Value.HasMemories.Should().BeTrue();

        // Без memories — отдельный экземпляр.
        var empty = MakeDeceased();
        deceasedRepo
            .Setup(x => x.GetByIdWithMemories(empty.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empty);
        var withoutMemories = await useCase.Execute(
            new HasMemoriesQuery(empty.Id), CancellationToken.None);
        withoutMemories.IsSuccess.Should().BeTrue();
        withoutMemories.Value.HasMemories.Should().BeFalse();

        // Pending не считается видимым.
        var pendingOnly = MakeDeceased();
        pendingOnly.AddMemory("Pending", CardAuthorId);
        deceasedRepo
            .Setup(x => x.GetByIdWithMemories(pendingOnly.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingOnly);
        var withPending = await useCase.Execute(
            new HasMemoriesQuery(pendingOnly.Id), CancellationToken.None);
        withPending.IsSuccess.Should().BeTrue();
        withPending.Value.HasMemories.Should().BeFalse();
    }

    /// <summary>
    /// E21: GetNearby happy — repo вызвался, use case вернул PagedResponse.
    /// Точная фильтрация по bbox + haversine тестируется в integration-тестах
    /// (нужна реальная БД, чтобы убедиться, что EF-перевод bounding-box
    /// предиката работает). Здесь — только что use case оркеструет вызов.
    /// </summary>
    [Fact]
    public async Task GetNearby_Authenticated_ReturnsPaged()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));

        deceasedRepo
            .Setup(x => x.GetNearby(It.IsAny<GetNearbyDeceasedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<NearbyDeceasedRow>(), 0));

        var fileStorage = new Mock<IFileStorage>();
        var useCase = new GetNearbyDeceasedUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetNearbyDeceasedQuery, GetNearbyDeceasedQueryValidator>());

        var result = await useCase.Execute(
            new GetNearbyDeceasedQuery(55.7558, 37.6173, RadiusMeters: 100, Page: 1, PageSize: 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        deceasedRepo.Verify(
            x => x.GetNearby(It.IsAny<GetNearbyDeceasedQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// E21: радиус за пределами [10, 5000] → Validation error,
    /// repo НЕ вызывается. Защита от случайных "поиск по всей планете".
    /// </summary>
    [Theory]
    [InlineData(5)]      // ниже минимума 10
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5001)]   // выше максимума 5000
    [InlineData(50000)]
    public async Task GetNearby_RadiusOutOfRange_ReturnsValidationError(int radius)
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));

        var fileStorage = new Mock<IFileStorage>();
        var useCase = new GetNearbyDeceasedUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetNearbyDeceasedQuery, GetNearbyDeceasedQueryValidator>());

        var result = await useCase.Execute(
            new GetNearbyDeceasedQuery(55.0, 37.0, radius, 1, 20),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        deceasedRepo.Verify(
            x => x.GetNearby(It.IsAny<GetNearbyDeceasedQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// E21: некорректная широта/долгота → Validation. Repo не вызывается.
    /// </summary>
    [Theory]
    [InlineData(91.0, 37.0)]    // lat > 90
    [InlineData(-91.0, 37.0)]   // lat < -90
    [InlineData(55.0, 181.0)]   // lon > 180
    [InlineData(55.0, -181.0)]  // lon < -180
    public async Task GetNearby_OutOfRangeCoords_ReturnsValidation(double lat, double lon)
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));

        var fileStorage = new Mock<IFileStorage>();
        var useCase = new GetNearbyDeceasedUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetNearbyDeceasedQuery, GetNearbyDeceasedQueryValidator>());

        var result = await useCase.Execute(
            new GetNearbyDeceasedQuery(lat, lon, 100, 1, 20),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    /// <summary>
    /// E21: маппинг row → response сохраняет DistanceMeters (округлённо)
    /// и адресные поля. Round trip 123.6м → 124 (округление к ближайшему).
    /// </summary>
    [Fact]
    public async Task GetNearby_MapsDistanceAndAddressFields()
    {
        var deceased = MakeDeceasedWithCoords(55.7560, 37.6175, city: "Москва", cemetery: "Test");

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));

        deceasedRepo
            .Setup(x => x.GetNearby(It.IsAny<GetNearbyDeceasedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<NearbyDeceasedRow> { new(deceased, 123.6) }, 1));

        var fileStorage = new Mock<IFileStorage>();
        var useCase = new GetNearbyDeceasedUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetNearbyDeceasedQuery, GetNearbyDeceasedQueryValidator>());

        var result = await useCase.Execute(
            new GetNearbyDeceasedQuery(55.7558, 37.6173, 100, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.Single();
        item.DistanceMeters.Should().Be(124);
        item.City.Should().Be("Москва");
        item.CemeteryName.Should().Be("Test");
        item.Latitude.Should().BeApproximately(55.7560, 0.000001);
    }

    private static Deceased MakeDeceasedWithCoords(
        double lat, double lon, string? city = null, string? cemetery = null)
    {
        var burial = BurialLocation.Create(
            latitude: lat,
            longitude: lon,
            city: city,
            cemeteryName: cemetery,
            accuracy: LocationAccuracy.Exact).Value;

        return Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            burial,
            CardAuthorId).Value;
    }

    private static Deceased MakeDeceased() =>
        Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, CardAuthorId).Value;
}
