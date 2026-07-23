using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions;
using GdeOni.Application.Subscriptions.Commands.CancelPendingPayment.UseCase;
using GdeOni.Domain.Aggregates.Subscriptions;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Subscriptions;

/// <summary>
/// D16. Юзер тапнул «Отменить» на PendingPayment. Проверяем цепочку:
/// поиск Pending → отмена в провайдере → локальный MarkCancelled →
/// откат <see cref="User.Subscription"/> в Trial/Expired.
/// </summary>
public sealed class CancelPendingPaymentUseCaseTests
{
    [Fact]
    public async Task Execute_NoPendingPayment_NoOpSuccess()
    {
        var (currentUser, userRepo, paymentRepo, provider, useCase) = BuildHarness();
        var userId = Guid.NewGuid();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId));
        paymentRepo
            .Setup(x => x.GetActivePendingForUser(
                userId, It.IsAny<TimeSpan>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPayment?)null);

        var result = await useCase.Execute(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        provider.Verify(
            x => x.CancelPaymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        userRepo.Verify(
            x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_HappyPath_CancelsAndRevertsToTrial()
    {
        var (currentUser, userRepo, paymentRepo, provider, useCase) = BuildHarness();
        var user = User.Register("alice@example.com", "hash$hash$hash$hash").Value;
        var nowUtc = DateTime.UtcNow;
        user.StartTrial(nowUtc, TimeSpan.FromDays(30));
        user.RequestSubscriptionPayment(SubscriptionPlan.Monthly, "pay-abc");

        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var pending = SubscriptionPayment.Create(
            user.Id, "pay-abc", SubscriptionPlan.Monthly, 49m,
            "https://yk/checkout/pay-abc", nowUtc).Value;
        paymentRepo
            .Setup(x => x.GetActivePendingForUser(
                user.Id, It.IsAny<TimeSpan>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        provider
            .Setup(x => x.CancelPaymentAsync("pay-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<Error>());

        var result = await useCase.Execute(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        pending.Status.Should().Be(PaymentRecordStatus.Cancelled);
        // ExpiresAtUtc trial ещё в будущем → возврат в Trial (доступ есть).
        user.Subscription.Status.Should().Be(SubscriptionStatus.Trial);
        user.Subscription.Plan.Should().BeNull();
        user.Subscription.LastPaymentId.Should().BeNull();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ProviderFails_DoesNotSave()
    {
        var (currentUser, userRepo, paymentRepo, provider, useCase) = BuildHarness();
        var userId = Guid.NewGuid();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId));

        var pending = SubscriptionPayment.Create(
            userId, "pay-fail", SubscriptionPlan.Monthly, 49m,
            "https://yk/checkout/pay-fail", DateTime.UtcNow).Value;
        paymentRepo
            .Setup(x => x.GetActivePendingForUser(
                userId, It.IsAny<TimeSpan>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        provider
            .Setup(x => x.CancelPaymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Errors.General.Failure(
                "payment.provider.network_error", "boom"));

        var result = await useCase.Execute(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
        pending.Status.Should().Be(PaymentRecordStatus.Pending);
    }

    private static (
        Mock<ICurrentUserService> CurrentUser,
        Mock<IUserRepository> UserRepo,
        Mock<ISubscriptionPaymentRepository> PaymentRepo,
        Mock<IPaymentProvider> Provider,
        CancelPendingPaymentUseCase UseCase) BuildHarness()
    {
        var currentUser = new Mock<ICurrentUserService>();
        var userRepo = new Mock<IUserRepository>();
        var paymentRepo = new Mock<ISubscriptionPaymentRepository>();
        var provider = new Mock<IPaymentProvider>();
        var options = Options.Create(new SubscriptionOptions());
        var useCase = new CancelPendingPaymentUseCase(
            currentUser.Object,
            userRepo.Object,
            paymentRepo.Object,
            provider.Object,
            options,
            TimeProvider.System);
        return (currentUser, userRepo, paymentRepo, provider, useCase);
    }
}
