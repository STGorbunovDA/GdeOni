using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.AdminRemoveUserTracking.Model;
using GdeOni.Application.Users.Commands.AdminRemoveUserTracking.UseCase;
using GdeOni.Application.Users.Commands.AdminRemoveUserTracking.Validation;
using GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.Model;
using GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.UseCase;
using GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.Validation;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Admin-tracking use case'ы (F17.7 mobile): снять одно отслеживание,
/// снять все, листинг отслеживаний юзера для админа. Контроллеры
/// гейтят Roles=SuperAdmin/Admin; use case'ы дополнительно отрезают
/// SuperAdmin-target как самозащиту.
/// </summary>
public sealed class AdminTrackingUseCaseTests
{
    // ─────────── AdminRemoveUserTrackingUseCase ───────────

    [Fact]
    public async Task RemoveOne_UserNotFound_ReturnsNotFound()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByIdWithTrackingByDeceasedId(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var useCase = new AdminRemoveUserTrackingUseCase(
            userRepo.Object,
            TestExecutor.With<AdminRemoveUserTrackingCommand, AdminRemoveUserTrackingCommandValidator>());

        var result = await useCase.Execute(
            new AdminRemoveUserTrackingCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.not.found");
    }

    [Fact]
    public async Task RemoveOne_TargetIsSuperAdmin_ReturnsUserForbidden()
    {
        var superAdmin = User.RegisterSuperAdmin("super@example.com", "$hash").Value;
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByIdWithTrackingByDeceasedId(
                superAdmin.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(superAdmin);

        var useCase = new AdminRemoveUserTrackingUseCase(
            userRepo.Object,
            TestExecutor.With<AdminRemoveUserTrackingCommand, AdminRemoveUserTrackingCommandValidator>());

        var result = await useCase.Execute(
            new AdminRemoveUserTrackingCommand(superAdmin.Id, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.forbidden");
    }

    [Fact]
    public async Task RemoveOne_Happy_RemovesAndSaves()
    {
        var user = User.Register("user@example.com", "$hash").Value;
        var deceasedId = Guid.NewGuid();
        user.TrackDeceased(deceasedId, RelationshipType.Friend);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByIdWithTrackingByDeceasedId(
                user.Id, deceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var useCase = new AdminRemoveUserTrackingUseCase(
            userRepo.Object,
            TestExecutor.With<AdminRemoveUserTrackingCommand, AdminRemoveUserTrackingCommandValidator>());

        var result = await useCase.Execute(
            new AdminRemoveUserTrackingCommand(user.Id, deceasedId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.TrackedDeceasedItems.Should().BeEmpty();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────── AdminRemoveAllUserTrackingUseCase ───────────

    [Fact]
    public async Task RemoveAll_UserNotFound_ReturnsNotFound()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByIdWithAllTracking(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var useCase = new AdminRemoveAllUserTrackingUseCase(
            userRepo.Object,
            TestExecutor.With<AdminRemoveAllUserTrackingCommand, AdminRemoveAllUserTrackingCommandValidator>());

        var result = await useCase.Execute(
            new AdminRemoveAllUserTrackingCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.not.found");
    }

    [Fact]
    public async Task RemoveAll_NoTrackings_ReturnsZeroAndSkipsSave()
    {
        var user = User.Register("user@example.com", "$hash").Value;
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByIdWithAllTracking(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var useCase = new AdminRemoveAllUserTrackingUseCase(
            userRepo.Object,
            TestExecutor.With<AdminRemoveAllUserTrackingCommand, AdminRemoveAllUserTrackingCommandValidator>());

        var result = await useCase.Execute(
            new AdminRemoveAllUserTrackingCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RemovedCount.Should().Be(0);
        // Если нечего удалять — Save не вызываем (нет dirty changes).
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAll_Happy_ReturnsCountAndSaves()
    {
        var user = User.Register("user@example.com", "$hash").Value;
        user.TrackDeceased(Guid.NewGuid(), RelationshipType.Friend);
        user.TrackDeceased(Guid.NewGuid(), RelationshipType.Friend);
        user.TrackDeceased(Guid.NewGuid(), RelationshipType.Friend);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByIdWithAllTracking(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var useCase = new AdminRemoveAllUserTrackingUseCase(
            userRepo.Object,
            TestExecutor.With<AdminRemoveAllUserTrackingCommand, AdminRemoveAllUserTrackingCommandValidator>());

        var result = await useCase.Execute(
            new AdminRemoveAllUserTrackingCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RemovedCount.Should().Be(3);
        user.TrackedDeceasedItems.Should().BeEmpty();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────── GetUserTrackedDeceasedForAdminUseCase ───────────

    [Fact]
    public async Task GetUserTracked_Empty_ReturnsEmptyPaged()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetMyTrackedDeceasedPaged(
                It.IsAny<Guid>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<(TrackedDeceased, Deceased)>(), 0));

        var useCase = new GetUserTrackedDeceasedForAdminUseCase(
            userRepo.Object,
            TestExecutor.With<GetUserTrackedDeceasedForAdminQuery, GetUserTrackedDeceasedForAdminQueryValidator>());

        var result = await useCase.Execute(
            new GetUserTrackedDeceasedForAdminQuery(Guid.NewGuid(), 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserTracked_Happy_MapsItems()
    {
        var user = User.Register("user@example.com", "$hash").Value;
        var deceased = Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, Guid.NewGuid()).Value;
        user.TrackDeceased(deceased.Id, RelationshipType.Friend);
        var tracking = user.TrackedDeceasedItems.First();

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetMyTrackedDeceasedPaged(
                user.Id, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new List<(TrackedDeceased Tracking, Deceased Deceased)>
                {
                    (tracking, deceased)
                },
                1));

        var useCase = new GetUserTrackedDeceasedForAdminUseCase(
            userRepo.Object,
            TestExecutor.With<GetUserTrackedDeceasedForAdminQuery, GetUserTrackedDeceasedForAdminQueryValidator>());

        var result = await useCase.Execute(
            new GetUserTrackedDeceasedForAdminQuery(user.Id, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        var item = result.Value.Items.Single();
        item.DeceasedId.Should().Be(deceased.Id);
        item.FullName.Should().Be("Иванов Иван");
        item.RelationshipType.Should().Be(nameof(RelationshipType.Friend));
    }
}
