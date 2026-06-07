using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Commands.CancelSubscription.Model;
using GdeOni.Application.Subscriptions.Commands.CancelSubscription.UseCase;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Subscriptions;

public sealed class CancelSubscriptionUseCaseTests
{
    [Fact]
    public async Task Execute_FromActive_CancelsAndSaves()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var user = User.Register("alice@example.com", "hash$hash$hash$hash").Value;
        var expiresAt = DateTime.UtcNow.AddDays(30);
        user.ActivateSubscription(SubscriptionPlan.Monthly, DateTime.UtcNow, expiresAt, "pay-1");
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(
            new CancelSubscriptionCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        // ExpiresAtUtc сохраняется — paid-period дорабатывает.
        user.Subscription.ExpiresAtUtc.Should().Be(expiresAt);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_FromNone_ReturnsNotCancellable()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var user = User.Register("alice@example.com", "hash$hash$hash$hash").Value;
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(
            new CancelSubscriptionCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.not_cancellable");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<ICurrentUserService> CurrentUser,
        CancelSubscriptionUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var useCase = new CancelSubscriptionUseCase(userRepo.Object, currentUser.Object, TimeProvider.System);
        return (userRepo, currentUser, useCase);
    }
}
