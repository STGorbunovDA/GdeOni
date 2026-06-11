using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.TrackDeceased.Model;
using GdeOni.Application.Users.Commands.TrackDeceased.UseCase;
using GdeOni.Application.Users.Commands.TrackDeceased.Validation;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты <see cref="TrackDeceasedUseCase"/> — POST на /api/users/me/tracked-deceased/{id}.
/// Покрываем: deceased не существует → 404, happy path → tracking создан.
/// </summary>
public sealed class TrackDeceasedUseCaseTests
{
    /// <summary>
    /// Деcеased не существует → NotFound. Use case проверяет это
    /// явно через ExistsById (не полагается на FK constraint в БД).
    /// </summary>
    [Fact]
    public async Task Execute_DeceasedDoesNotExist_ReturnsNotFound()
    {
        var user = User.Register("user@example.com", "$hash").Value;

        var userRepo = new Mock<IUserRepository>();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo
            .Setup(x => x.GetByIdWithTrackingByDeceasedId(
                user.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        deceasedRepo
            .Setup(x => x.ExistsById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new TrackDeceasedUseCase(
            userRepo.Object,
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<TrackDeceasedCommand, TrackDeceasedCommandValidator>());

        var result = await useCase.Execute(
            new TrackDeceasedCommand(
                Guid.NewGuid(), RelationshipType.Friend,
                null, false, false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.not.found");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Happy path: пользователь и deceased существуют → tracking
    /// создан со статусом Active, Save вызван.
    /// </summary>
    [Fact]
    public async Task Execute_HappyPath_AddsTrackingAndSaves()
    {
        var user = User.Register("user@example.com", "$hash").Value;
        var deceasedId = Guid.NewGuid();

        var userRepo = new Mock<IUserRepository>();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo
            .Setup(x => x.GetByIdWithTrackingByDeceasedId(
                user.Id, deceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        deceasedRepo
            .Setup(x => x.ExistsById(deceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new TrackDeceasedUseCase(
            userRepo.Object,
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<TrackDeceasedCommand, TrackDeceasedCommandValidator>());

        var result = await useCase.Execute(
            new TrackDeceasedCommand(
                deceasedId, RelationshipType.Friend,
                "Заметка", true, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DeceasedId.Should().Be(deceasedId);
        user.TrackedDeceasedItems.Should().HaveCount(1);
        user.TrackedDeceasedItems.Single().Status.Should().Be(TrackStatus.Active);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }
}
