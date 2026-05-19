using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions;
using GdeOni.Application.Subscriptions.Queries.GetMySubscription.UseCase;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Subscriptions;

/// <summary>
/// D16. Тесты <see cref="GetMySubscriptionUseCase"/> — собирает
/// DTO с Status/Plan/IsActiveNow/IsOnTrial/DaysUntilExpiry.
/// </summary>
public sealed class GetMySubscriptionUseCaseTests
{
    [Fact]
    public async Task Execute_NotAuthenticated_ReturnsUnauthorized()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Errors.General.Unauthorized());

        var result = await useCase.Execute(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Execute_UserNotFound_ReturnsNotFound()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var userId = Guid.NewGuid();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId));
        userRepo.Setup(x => x.GetByIdReadOnly(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await useCase.Execute(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Execute_UserOnTrial_ReturnsTrialResponse()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var user = BuildUserWithTrial();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetByIdReadOnly(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Trial");
        result.Value.Plan.Should().BeNull();
        result.Value.IsActiveNow.Should().BeTrue();
        result.Value.IsOnTrial.Should().BeTrue();
        result.Value.DaysUntilExpiry.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Execute_NoneSubscription_ReturnsInactive()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var user = User.Register("ivan@example.com", "hash$hash$hash$hash").Value;
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetByIdReadOnly(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("None");
        result.Value.IsActiveNow.Should().BeFalse();
        result.Value.IsOnTrial.Should().BeFalse();
        result.Value.DaysUntilExpiry.Should().Be(0);
    }

    private static User BuildUserWithTrial()
    {
        var user = User.Register("alice@example.com", "hash$hash$hash$hash").Value;
        user.StartTrial(DateTime.UtcNow, TimeSpan.FromDays(30));
        return user;
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<ICurrentUserService> CurrentUser,
        GetMySubscriptionUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var useCase = new GetMySubscriptionUseCase(userRepo.Object, currentUser.Object);
        return (userRepo, currentUser, useCase);
    }
}
