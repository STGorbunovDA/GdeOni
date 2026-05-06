using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
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
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.Model;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты Deceased query use case'ов: GetAll (admin only), GetById (admin
/// видит все memories), GetDistance, GetAgeAtDeath, HasMemories.
/// </summary>
public sealed class DeceasedQueriesTests
{
    private static readonly Guid CardAuthorId = Guid.NewGuid();

    /// <summary>
    /// GetAll не-admin → InsufficientPermissionsToViewAllDeceased.
    /// </summary>
    [Fact]
    public async Task GetAll_NotAdmin_ReturnsForbidden()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        var useCase = new GetAllDeceasedUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<GetAllDeceasedQuery, GetAllDeceasedQueryValidator>());

        var result = await useCase.Execute(
            new GetAllDeceasedQuery(null, null, null, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.insufficient_permissions.view_all");
    }

    /// <summary>
    /// GetAll admin → success + GetPaged вызван.
    /// </summary>
    [Fact]
    public async Task GetAll_Admin_ReturnsPaged()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo
            .Setup(x => x.GetPaged(It.IsAny<GetAllDeceasedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Deceased>(), 0));

        var useCase = new GetAllDeceasedUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<GetAllDeceasedQuery, GetAllDeceasedQueryValidator>());

        var result = await useCase.Execute(
            new GetAllDeceasedQuery(null, null, null, null, null, null, 1, 10),
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

        var useCase = new GetDeceasedByIdUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<GetDeceasedByIdQuery, GetDeceasedByIdQueryValidator>());

        var result = await useCase.Execute(
            new GetDeceasedByIdQuery(deceased.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanSeeAllMemories.Should().BeTrue();
    }

    /// <summary>
    /// GetById outsider (не admin, не автор) → CanSeeAllMemories=false.
    /// </summary>
    [Fact]
    public async Task GetById_Outsider_CanNotSeeAllMemories()
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

        var useCase = new GetDeceasedByIdUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<GetDeceasedByIdQuery, GetDeceasedByIdQueryValidator>());

        var result = await useCase.Execute(
            new GetDeceasedByIdQuery(deceased.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanSeeAllMemories.Should().BeFalse();
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
        deceased.AddMemory("Текст", CardAuthorId);

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
    }

    private static Deceased MakeDeceased() =>
        Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, CardAuthorId).Value;
}
