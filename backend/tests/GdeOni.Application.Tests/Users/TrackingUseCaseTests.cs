using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.RemoveTracking.Model;
using GdeOni.Application.Users.Commands.RemoveTracking.UseCase;
using GdeOni.Application.Users.Commands.RemoveTracking.Validation;
using GdeOni.Application.Users.Commands.UpdateTracking.Model;
using GdeOni.Application.Users.Commands.UpdateTracking.UseCase;
using GdeOni.Application.Users.Commands.UpdateTracking.Validation;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты Update/Remove Tracking — отслеживание умерших.
/// Объединил в один файл, общая инфра (User + filtered Include).
/// </summary>
public sealed class TrackingUseCaseTests
{
    /// <summary>
    /// UpdateTracking: tracking не найдено для текущего user'а →
    /// Tracking.NotFound. Save не вызывается.
    /// </summary>
    [Fact]
    public async Task UpdateTracking_NotFound_ReturnsTrackingNotFound()
    {
        var user = User.Register("alice@example.com", "hash").Value;

        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetByIdWithTrackingByDeceasedId(
                user.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user); // user есть, но без tracking'ов.

        var useCase = new UpdateTrackingUseCase(
            userRepo.Object, currentUser.Object,
            TestExecutor.With<UpdateTrackingCommand, UpdateTrackingCommandValidator>());

        var result = await useCase.Execute(
            new UpdateTrackingCommand(
                Guid.NewGuid(),
                RelationshipType.Friend,
                "notes",
                new[] { 0 }, Array.Empty<int>(),
                TrackStatus.Active),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.not.found");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UpdateTracking happy: меняет relationship, notifications и
    /// статус, Save вызывается.
    /// </summary>
    [Fact]
    public async Task UpdateTracking_Happy_UpdatesAndSaves()
    {
        var user = User.Register("alice@example.com", "hash").Value;
        var deceasedId = Guid.NewGuid();
        user.TrackDeceased(deceasedId, RelationshipType.Friend);

        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetByIdWithTrackingByDeceasedId(
                user.Id, deceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var useCase = new UpdateTrackingUseCase(
            userRepo.Object, currentUser.Object,
            TestExecutor.With<UpdateTrackingCommand, UpdateTrackingCommandValidator>());

        var result = await useCase.Execute(
            new UpdateTrackingCommand(
                deceasedId,
                RelationshipType.Sibling,
                "Brother",
                DeathAnniversaryLeadDays: new[] { 0, 7 },
                BirthAnniversaryLeadDays: new[] { 0 },
                TrackStatus: TrackStatus.Muted),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var tracking = user.GetTracking(deceasedId)!;
        tracking.RelationshipType.Should().Be(RelationshipType.Sibling);
        tracking.PersonalNotes.Should().Be("Brother");
        tracking.NotifyOnDeathAnniversary.Should().BeTrue();
        tracking.NotifyOnBirthAnniversary.Should().BeTrue();
        tracking.DeathAnniversaryLeadDays.Should().BeEquivalentTo(new[] { 0, 7 });
        tracking.BirthAnniversaryLeadDays.Should().BeEquivalentTo(new[] { 0 });
        tracking.Status.Should().Be(TrackStatus.Muted);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// RemoveTracking happy: tracking удалён, Save вызывается.
    /// </summary>
    [Fact]
    public async Task RemoveTracking_Happy_RemovesAndSaves()
    {
        var user = User.Register("alice@example.com", "hash").Value;
        var deceasedId = Guid.NewGuid();
        user.TrackDeceased(deceasedId, RelationshipType.Friend);

        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetByIdWithTrackingByDeceasedId(
                user.Id, deceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var useCase = new RemoveTrackingUseCase(
            userRepo.Object, currentUser.Object,
            TestExecutor.With<RemoveTrackingCommand, RemoveTrackingCommandValidator>());

        var result = await useCase.Execute(
            new RemoveTrackingCommand(deceasedId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.TrackedDeceasedItems.Should().BeEmpty();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// RemoveTracking на несуществующем deceasedId → Tracking.NotFound.
    /// </summary>
    [Fact]
    public async Task RemoveTracking_NotFound_ReturnsTrackingNotFound()
    {
        var user = User.Register("alice@example.com", "hash").Value;

        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetByIdWithTrackingByDeceasedId(
                user.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var useCase = new RemoveTrackingUseCase(
            userRepo.Object, currentUser.Object,
            TestExecutor.With<RemoveTrackingCommand, RemoveTrackingCommandValidator>());

        var result = await useCase.Execute(
            new RemoveTrackingCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.not.found");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }
}
