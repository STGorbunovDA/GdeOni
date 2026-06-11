using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Queries.GetAll.Model;
using GdeOni.Application.Users.Queries.GetAll.UseCase;
using GdeOni.Application.Users.Queries.GetAll.Validation;
using GdeOni.Application.Users.Queries.GetById.Model;
using GdeOni.Application.Users.Queries.GetById.UseCase;
using GdeOni.Application.Users.Queries.GetById.Validation;
using GdeOni.Application.Users.Queries.GetCurrent.UseCase;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.UseCase;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Validation;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.Model;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.UseCase;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.Validation;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты query-use-case'ов: GetAll/GetById/GetCurrent users +
/// GetMyTrackedDeceased List / Details (только свои tracking).
/// </summary>
public sealed class UserQueriesTests
{
    /// <summary>
    /// GetAll не-admin → InsufficientPermissionsToViewAllUsers.
    /// </summary>
    [Fact]
    public async Task GetAll_NotAdmin_ReturnsForbidden()
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        var useCase = new GetAllUsersUseCase(
            userRepo.Object, currentUser.Object,
            TestExecutor.With<GetAllUsersQuery>(
                new GetAllUsersQueryValidator(TimeProvider.System)));

        var result = await useCase.Execute(
            new GetAllUsersQuery(null, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.insufficient_permissions.view_all");
    }

    /// <summary>
    /// GetAll admin → success + GetPaged вызван.
    /// </summary>
    [Fact]
    public async Task GetAll_Admin_ReturnsPaged()
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        userRepo
            .Setup(x => x.GetPaged(It.IsAny<GetAllUsersQuery>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<(User, int)>(), 0));

        var useCase = new GetAllUsersUseCase(
            userRepo.Object, currentUser.Object,
            TestExecutor.With<GetAllUsersQuery>(
                new GetAllUsersQueryValidator(TimeProvider.System)));

        var result = await useCase.Execute(
            new GetAllUsersQuery(null, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        userRepo.Verify(
            x => x.GetPaged(It.IsAny<GetAllUsersQuery>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// GetById выгружает (User, TrackingCount) и отдаёт TrackingCount
    /// в response.
    /// </summary>
    [Fact]
    public async Task GetById_ReturnsTrackingCount()
    {
        var user = User.Register("alice@example.com", "hash").Value;

        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo
            .Setup(x => x.GetByIdWithTrackingCount(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((user, 5));

        var useCase = new GetUserByIdUseCase(
            userRepo.Object, currentUser.Object,
            TestExecutor.With<GetUserByIdQuery, GetUserByIdQueryValidator>(),
            TimeProvider.System);

        var result = await useCase.Execute(
            new GetUserByIdQuery(user.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TrackingCount.Should().Be(5);
    }

    /// <summary>
    /// GetById outsider (не self, не admin) → UserForbidden.
    /// </summary>
    [Fact]
    public async Task GetById_Outsider_ReturnsForbidden()
    {
        var target = User.Register("bob@example.com", "hash").Value;

        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo
            .Setup(x => x.GetByIdWithTrackingCount(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((target, 0));

        var useCase = new GetUserByIdUseCase(
            userRepo.Object, currentUser.Object,
            TestExecutor.With<GetUserByIdQuery, GetUserByIdQueryValidator>(),
            TimeProvider.System);

        var result = await useCase.Execute(
            new GetUserByIdQuery(target.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.forbidden");
    }

    /// <summary>
    /// GetCurrent: GetCurrentUserId.IsFailure → возвращает ту же ошибку
    /// (Unauthorized из CurrentUserService).
    /// </summary>
    [Fact]
    public async Task GetCurrent_NoAuth_ReturnsCurrentUserError()
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Failure<Guid, Error>(Errors.General.Unauthorized()));

        var useCase = new GetCurrentUserUseCase(
            userRepo.Object,
            currentUser.Object,
            Microsoft.Extensions.Options.Options.Create(new GdeOni.Application.Legal.LegalOptions()));

        var result = await useCase.Execute(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    /// <summary>
    /// GetCurrent happy: User существует → success.
    /// </summary>
    [Fact]
    public async Task GetCurrent_Auth_ReturnsUser()
    {
        var user = User.Register("alice@example.com", "hash").Value;

        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo
            .Setup(x => x.GetByIdReadOnly(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var useCase = new GetCurrentUserUseCase(
            userRepo.Object,
            currentUser.Object,
            Microsoft.Extensions.Options.Options.Create(new GdeOni.Application.Legal.LegalOptions()));

        var result = await useCase.Execute(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("alice@example.com");
    }

    /// <summary>
    /// GetMyTrackedDeceasedList: возвращает только свои tracking
    /// (репо уже фильтрует по userId — мы проверяем что use case
    /// зовёт его с currentUserId и формирует ответ).
    /// </summary>
    [Fact]
    public async Task GetMyTrackedDeceasedList_ReturnsOnlyOwn()
    {
        var userId = Guid.NewGuid();
        var deceased = Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, Guid.NewGuid()).Value;
        var tracked = TrackedDeceased.Create(deceased.Id, RelationshipType.Friend).Value;

        var userRepo = new Mock<IUserRepository>();
        var fileStorage = new Mock<IFileStorage>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId));
        userRepo
            .Setup(x => x.GetMyTrackedDeceasedPaged(
                userId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new List<(TrackedDeceased Tracking, Deceased Deceased)> { (tracked, deceased) },
                1));

        var useCase = new GetMyTrackedDeceasedListUseCase(
            userRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetMyTrackedDeceasedListQuery, GetMyTrackedDeceasedListQueryValidator>());

        var result = await useCase.Execute(
            new GetMyTrackedDeceasedListQuery(1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.Single().DeceasedId.Should().Be(deceased.Id);
        userRepo.Verify(
            x => x.GetMyTrackedDeceasedPaged(userId, 1, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// GetMyTrackedDeceasedDetails: tracking не существует → NotTracked (403).
    /// </summary>
    [Fact]
    public async Task GetMyTrackedDeceasedDetails_NotTracked_ReturnsNotTracked()
    {
        var user = User.Register("alice@example.com", "hash").Value;

        var userRepo = new Mock<IUserRepository>();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo
            .Setup(x => x.GetByIdWithTrackingByDeceasedId(
                user.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var fileStorage = new Mock<IFileStorage>();
        var useCase = new GetMyTrackedDeceasedDetailsUseCase(
            userRepo.Object, deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetMyTrackedDeceasedDetailsQuery, GetMyTrackedDeceasedDetailsQueryValidator>());

        var result = await useCase.Execute(
            new GetMyTrackedDeceasedDetailsQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.not_tracked");
    }

    /// <summary>
    /// GetMyTrackedDeceasedDetails happy: tracking существует, deceased
    /// найден → success.
    /// </summary>
    [Fact]
    public async Task GetMyTrackedDeceasedDetails_Happy_Returns()
    {
        var user = User.Register("alice@example.com", "hash").Value;
        var deceased = Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, Guid.NewGuid()).Value;
        user.TrackDeceased(deceased.Id, RelationshipType.Friend);

        var userRepo = new Mock<IUserRepository>();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo
            .Setup(x => x.GetByIdWithTrackingByDeceasedId(
                user.Id, deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        deceasedRepo
            .Setup(x => x.GetByIdWithMemoriesReadOnly(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var fileStorage = new Mock<IFileStorage>();
        var useCase = new GetMyTrackedDeceasedDetailsUseCase(
            userRepo.Object, deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetMyTrackedDeceasedDetailsQuery, GetMyTrackedDeceasedDetailsQueryValidator>());

        var result = await useCase.Execute(
            new GetMyTrackedDeceasedDetailsQuery(deceased.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Deceased.Should().Be(deceased);
        result.Value.Tracking.Should().NotBeNull();
    }
}
