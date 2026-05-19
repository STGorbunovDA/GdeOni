using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Subscriptions;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.Model;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.UseCase;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Subscriptions;

public sealed class ProcessPaymentWebhookUseCaseTests
{
    [Fact]
    public async Task Execute_InvalidSignature_ReturnsUnauthorized()
    {
        var (userRepo, paymentProvider, useCase) = BuildHarness();
        paymentProvider
            .Setup(x => x.VerifyWebhookAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PaymentVerification, Error>(
                Errors.Subscription.InvalidPaymentSignature()));

        var result = await useCase.Execute(
            new ProcessPaymentWebhookCommand("{}", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.payment.invalid_signature");
        // БД не дёргаем при невалидной подписи — защита от replay
        // через GetBySubscriptionPaymentId.
        userRepo.Verify(
            x => x.GetBySubscriptionPaymentId(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_PaymentNotFound_ReturnsNotFound()
    {
        var (userRepo, paymentProvider, useCase) = BuildHarness();
        paymentProvider
            .Setup(x => x.VerifyWebhookAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PaymentVerification, Error>(
                new PaymentVerification("pay-unknown", PaymentStatus.Succeeded, 49m)));
        userRepo
            .Setup(x => x.GetBySubscriptionPaymentId("pay-unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await useCase.Execute(
            new ProcessPaymentWebhookCommand("{}", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.payment.not_found");
    }

    [Fact]
    public async Task Execute_SucceededWebhook_ActivatesUserAndSaves()
    {
        var (userRepo, paymentProvider, useCase) = BuildHarness();
        var user = User.Register("ivan@example.com", "hash$hash$hash$hash").Value;
        user.StartTrial(DateTime.UtcNow, TimeSpan.FromDays(30));
        user.RequestSubscriptionPayment(SubscriptionPlan.Monthly, "pay-1");

        paymentProvider
            .Setup(x => x.VerifyWebhookAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PaymentVerification, Error>(
                new PaymentVerification("pay-1", PaymentStatus.Succeeded, 49m)));
        userRepo
            .Setup(x => x.GetBySubscriptionPaymentId("pay-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(
            new ProcessPaymentWebhookCommand("{}", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Subscription.Status.Should().Be(SubscriptionStatus.Active);
        user.Subscription.LastPaymentId.Should().Be("pay-1");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_PendingStatus_DoesNotChangeUser()
    {
        var (userRepo, paymentProvider, useCase) = BuildHarness();
        var user = User.Register("ivan@example.com", "hash$hash$hash$hash").Value;
        user.StartTrial(DateTime.UtcNow, TimeSpan.FromDays(30));
        user.RequestSubscriptionPayment(SubscriptionPlan.Monthly, "pay-1");

        paymentProvider
            .Setup(x => x.VerifyWebhookAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PaymentVerification, Error>(
                new PaymentVerification("pay-1", PaymentStatus.Pending, 49m)));
        userRepo
            .Setup(x => x.GetBySubscriptionPaymentId("pay-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(
            new ProcessPaymentWebhookCommand("{}", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Subscription.Status.Should().Be(SubscriptionStatus.PendingPayment);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<IPaymentProvider> PaymentProvider,
        ProcessPaymentWebhookUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var paymentProvider = new Mock<IPaymentProvider>();
        var options = Options.Create(new SubscriptionOptions());
        var useCase = new ProcessPaymentWebhookUseCase(
            userRepo.Object, paymentProvider.Object, options);
        return (userRepo, paymentProvider, useCase);
    }
}
