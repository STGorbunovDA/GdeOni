using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.Model;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.UseCase;
using GdeOni.Application.Subscriptions.Commands.SyncSubscription.UseCase;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Subscriptions;

/// <summary>
/// D16. Тесты <see cref="SyncSubscriptionUseCase"/> — pull-fallback
/// вместо webhook'а. Проверяем, что use case находит свежий Pending,
/// делегирует в webhook-хендлер, и no-op'ит когда синхронизировать
/// нечего.
/// </summary>
public sealed class SyncSubscriptionUseCaseTests
{
    [Fact]
    public async Task Execute_NoPendingPayment_NoOp()
    {
        var (currentUser, paymentRepo, webhook, useCase) = BuildHarness();
        var userId = Guid.NewGuid();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId));
        paymentRepo
            .Setup(x => x.GetActivePendingForUser(
                userId, It.IsAny<TimeSpan>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdeOni.Domain.Aggregates.Subscriptions.SubscriptionPayment?)null);

        var result = await useCase.Execute(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        webhook.Verify(
            x => x.Execute(
                It.IsAny<ProcessPaymentWebhookCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_HasPendingPayment_DelegatesToWebhook_WithPaymentId()
    {
        var (currentUser, paymentRepo, webhook, useCase) = BuildHarness();
        var userId = Guid.NewGuid();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId));

        var pending = GdeOni.Domain.Aggregates.Subscriptions.SubscriptionPayment
            .Create(userId, "pay-abc", SubscriptionPlan.Monthly, 49m,
                "https://yk/checkout/pay-abc", DateTime.UtcNow)
            .Value;
        paymentRepo
            .Setup(x => x.GetActivePendingForUser(
                userId, It.IsAny<TimeSpan>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        ProcessPaymentWebhookCommand? capturedCommand = null;
        webhook
            .Setup(x => x.Execute(
                It.IsAny<ProcessPaymentWebhookCommand>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProcessPaymentWebhookCommand, CancellationToken>(
                (cmd, _) => capturedCommand = cmd)
            .ReturnsAsync(UnitResult.Success<Error>());

        var result = await useCase.Execute(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Payload.Should().Contain("pay-abc");
        capturedCommand.SignatureHeader.Should().BeNull();
    }

    [Fact]
    public async Task Execute_CurrentUserFails_ReturnsError()
    {
        var (currentUser, _, webhook, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Errors.General.Unauthorized());

        var result = await useCase.Execute(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        webhook.Verify(
            x => x.Execute(
                It.IsAny<ProcessPaymentWebhookCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_WebhookFails_PropagatesError()
    {
        var (currentUser, paymentRepo, webhook, useCase) = BuildHarness();
        var userId = Guid.NewGuid();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId));

        var pending = GdeOni.Domain.Aggregates.Subscriptions.SubscriptionPayment
            .Create(userId, "pay-xyz", SubscriptionPlan.Monthly, 49m,
                "https://yk/checkout/pay-xyz", DateTime.UtcNow)
            .Value;
        paymentRepo
            .Setup(x => x.GetActivePendingForUser(
                userId, It.IsAny<TimeSpan>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        webhook
            .Setup(x => x.Execute(
                It.IsAny<ProcessPaymentWebhookCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Errors.General.Failure(
                "payment.provider.network_error", "YooKassa is unreachable."));

        var result = await useCase.Execute(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payment.provider.network_error");
    }

    private static (
        Mock<ICurrentUserService> CurrentUser,
        Mock<ISubscriptionPaymentRepository> PaymentRepo,
        Mock<IProcessPaymentWebhookUseCase> Webhook,
        SyncSubscriptionUseCase UseCase) BuildHarness()
    {
        var currentUser = new Mock<ICurrentUserService>();
        var paymentRepo = new Mock<ISubscriptionPaymentRepository>();
        var webhook = new Mock<IProcessPaymentWebhookUseCase>();
        var options = Options.Create(new SubscriptionOptions());
        var useCase = new SyncSubscriptionUseCase(
            currentUser.Object,
            paymentRepo.Object,
            webhook.Object,
            options,
            TimeProvider.System);
        return (currentUser, paymentRepo, webhook, useCase);
    }
}
