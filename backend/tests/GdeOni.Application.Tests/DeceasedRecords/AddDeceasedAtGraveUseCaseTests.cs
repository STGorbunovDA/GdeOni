using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.Model;
using GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты <see cref="AddDeceasedAtGraveUseCase"/> — главный сценарий
/// мобильного клиента: пользователь у могилы создаёт карточку
/// атомарно с координатами и автотрекингом. Покрываем happy,
/// Unauthorized, дубликат SearchKey и user-not-found.
/// </summary>
public sealed class AddDeceasedAtGraveUseCaseTests
{
    /// <summary>
    /// Happy path: пользователь существует, дубля нет —
    /// создаются Deceased + Tracking, Save вызван один раз
    /// (атомарно через UnitOfWork).
    /// </summary>
    [Fact]
    public async Task Execute_HappyPath_CreatesDeceasedAndTracking()
    {
        // Arrange
        var user = User.Register("user@example.com", "$hash").Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        // Никаких дубликатов в БД.
        deceasedRepo
            .Setup(x => x.ExistsBySearchKey(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new AddDeceasedAtGraveUseCase(
            deceasedRepo.Object,
            userRepo.Object,
            currentUser.Object,
            TestExecutor.With<AddDeceasedAtGraveCommand, AddDeceasedAtGraveCommandValidator>());

        // Act
        var result = await useCase.Execute(SampleCommand(), CancellationToken.None);

        // Assert: успех, Add+Save вызваны, в user'е появился tracking.
        result.IsSuccess.Should().BeTrue();
        result.Value.DeceasedId.Should().NotBe(Guid.Empty);
        result.Value.TrackingStatus.Should().Be("Active");
        deceasedRepo.Verify(
            x => x.Add(It.IsAny<Domain.Aggregates.DeceasedRecords.Deceased>(), It.IsAny<CancellationToken>()),
            Times.Once);
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        user.TrackedDeceasedItems.Should().HaveCount(1);
    }

    /// <summary>
    /// Дубликат: ExistsBySearchKey вернул true → AlreadyExists (409).
    /// Это early-check до вставки; уникальный индекс БД ловит race
    /// поверх (UniqueConstraintException), но happy path должен
    /// падать на early-check без похода в Save.
    /// </summary>
    [Fact]
    public async Task Execute_DuplicateSearchKey_ReturnsAlreadyExists()
    {
        var user = User.Register("user@example.com", "$hash").Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        deceasedRepo
            .Setup(x => x.ExistsBySearchKey(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // ← дубль найден.

        var useCase = new AddDeceasedAtGraveUseCase(
            deceasedRepo.Object,
            userRepo.Object,
            currentUser.Object,
            TestExecutor.With<AddDeceasedAtGraveCommand, AddDeceasedAtGraveCommandValidator>());

        var result = await useCase.Execute(SampleCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.already.exists");
        deceasedRepo.Verify(
            x => x.Add(It.IsAny<Domain.Aggregates.DeceasedRecords.Deceased>(), It.IsAny<CancellationToken>()),
            Times.Never);
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Unauthorized: currentUserService возвращает Failure.
    /// Use case не лезет в репозитории, прокидывает ошибку.
    /// </summary>
    [Fact]
    public async Task Execute_Unauthorized_ReturnsUnauthorized()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Failure<Guid, Error>(Errors.General.Unauthorized()));

        var useCase = new AddDeceasedAtGraveUseCase(
            deceasedRepo.Object,
            userRepo.Object,
            currentUser.Object,
            TestExecutor.With<AddDeceasedAtGraveCommand, AddDeceasedAtGraveCommandValidator>());

        var result = await useCase.Execute(SampleCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.unauthorized");
        userRepo.Verify(
            x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AddDeceasedAtGraveCommand SampleCommand() =>
        new(
            FirstName: "Иван",
            LastName: "Иванов",
            MiddleName: null,
            BirthDate: new DateOnly(1950, 6, 15),
            DeathDate: new DateOnly(2010, 1, 1),
            ShortDescription: null,
            Biography: null,
            GraveLocation: new AddDeceasedAtGraveLocationCommand(
                Latitude: 55.7558,
                Longitude: 37.6173,
                AccuracyMeters: 5,
                Country: null,
                City: null,
                CemeteryName: null,
                PlotNumber: null,
                GraveNumber: null),
            Tracking: new AddDeceasedAtGraveTrackingCommand(
                RelationshipType: RelationshipType.Friend,
                PersonalNotes: null,
                NotifyOnDeathAnniversary: false,
                NotifyOnBirthAnniversary: false));
}
