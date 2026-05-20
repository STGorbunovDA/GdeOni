using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Subscriptions;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.Model;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.UseCase;
using GdeOni.Domain.Aggregates.Subscriptions;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Subscriptions;

public sealed class ProcessPaymentWebhookUseCaseTests
{
    [Fact]
    public async Task Execute_InvalidSignature_ReturnsUnauthorized()
    {
        var (userRepo, paymentRepo, paymentProvider, useCase) = BuildHarness();
        paymentProvider
            .Setup(x => x.VerifyWebhookAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PaymentVerification, Error>(
                Errors.Subscription.InvalidPaymentSignature()));

        var result = await useCase.Execute(
            new ProcessPaymentWebhookCommand("{}", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.payment.invalid_signature");
        // БД не дёргаем при невалидной подписи — защита от replay.
        paymentRepo.Verify(
            x => x.GetByExternalPaymentId(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_PaymentNotFoundInHistory_ReturnsNotFound()
    {
        var (userRepo, paymentRepo, paymentProvider, useCase) = BuildHarness();
        paymentProvider
            .Setup(x => x.VerifyWebhookAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PaymentVerification, Error>(
                new PaymentVerification("pay-unknown", PaymentStatus.Succeeded, 49m)));
        paymentRepo
            .Setup(x => x.GetByExternalPaymentId("pay-unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPayment?)null);

        var result = await useCase.Execute(
            new ProcessPaymentWebhookCommand("{}", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.payment.not_found");
    }

    [Fact]
    public async Task Execute_SucceededWebhook_ActivatesUserAndMarksPaymentSucceeded()
    {
        var (userRepo, paymentRepo, paymentProvider, useCase) = BuildHarness();
        var user = User.Register("ivan@example.com", "hash$hash$hash$hash").Value;
        user.StartTrial(DateTime.UtcNow, TimeSpan.FromDays(30));
        user.RequestSubscriptionPayment(SubscriptionPlan.Monthly, "pay-1");

        var payment = SubscriptionPayment.Create(
            user.Id, "pay-1", SubscriptionPlan.Monthly, 49m, "https://yk/checkout/pay-1",
            DateTime.UtcNow.AddMinutes(-1)).Value;

        paymentProvider
            .Setup(x => x.VerifyWebhookAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PaymentVerification, Error>(
                new PaymentVerification("pay-1", PaymentStatus.Succeeded, 49m)));
        paymentRepo
            .Setup(x => x.GetByExternalPaymentId("pay-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        userRepo
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(
            new ProcessPaymentWebhookCommand("{}", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Subscription.Status.Should().Be(SubscriptionStatus.Active);
        user.Subscription.LastPaymentId.Should().Be("pay-1");
        payment.Status.Should().Be(PaymentRecordStatus.Succeeded);
        payment.PeriodStartUtc.Should().NotBeNull();
        payment.PeriodEndUtc.Should().NotBeNull();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_CancelledWebhook_MarksPaymentCancelled_KeepsUserSubscription()
    {
        var (userRepo, paymentRepo, paymentProvider, useCase) = BuildHarness();
        var user = User.Register("ivan@example.com", "hash$hash$hash$hash").Value;
        user.StartTrial(DateTime.UtcNow, TimeSpan.FromDays(30));
        user.RequestSubscriptionPayment(SubscriptionPlan.Monthly, "pay-c");

        var payment = SubscriptionPayment.Create(
            user.Id, "pay-c", SubscriptionPlan.Monthly, 49m, "https://yk/checkout/pay-c",
            DateTime.UtcNow.AddMinutes(-1)).Value;

        paymentProvider
            .Setup(x => x.VerifyWebhookAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PaymentVerification, Error>(
                new PaymentVerification("pay-c", PaymentStatus.Cancelled, 49m)));
        paymentRepo
            .Setup(x => x.GetByExternalPaymentId("pay-c", It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        userRepo
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(
            new ProcessPaymentWebhookCommand("{}", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentRecordStatus.Cancelled);
        // User остался в PendingPayment — может попробовать ещё раз.
        user.Subscription.Status.Should().Be(SubscriptionStatus.PendingPayment);
    }

    [Fact]
    public async Task Execute_PendingStatus_DoesNotChangeAnything()
    {
        var (userRepo, paymentRepo, paymentProvider, useCase) = BuildHarness();

        paymentProvider
            .Setup(x => x.VerifyWebhookAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PaymentVerification, Error>(
                new PaymentVerification("pay-1", PaymentStatus.Pending, 49m)));

        var payment = SubscriptionPayment.Create(
            Guid.NewGuid(), "pay-1", SubscriptionPlan.Monthly, 49m, "https://yk/p1",
            DateTime.UtcNow).Value;
        paymentRepo
            .Setup(x => x.GetByExternalPaymentId("pay-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var result = await useCase.Execute(
            new ProcessPaymentWebhookCommand("{}", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentRecordStatus.Pending);
        userRepo.Verify(x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<ISubscriptionPaymentRepository> PaymentRepo,
        Mock<IPaymentProvider> PaymentProvider,
        ProcessPaymentWebhookUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var paymentRepo = new Mock<ISubscriptionPaymentRepository>();
        var paymentProvider = new Mock<IPaymentProvider>();
        var options = Options.Create(new SubscriptionOptions());
        var useCase = new ProcessPaymentWebhookUseCase(
            userRepo.Object, paymentRepo.Object, paymentProvider.Object, options);
        return (userRepo, paymentRepo, paymentProvider, useCase);
    }
}
