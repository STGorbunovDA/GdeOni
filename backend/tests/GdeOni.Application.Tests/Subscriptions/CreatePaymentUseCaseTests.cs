using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.Model;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.UseCase;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Subscriptions;

/// <summary>
/// D16. Тесты <see cref="CreatePaymentUseCase"/>: создание платежа,
/// обработка ошибок провайдера, фиксация PendingPayment.
/// </summary>
public sealed class CreatePaymentUseCaseTests
{
    [Fact]
    public async Task Execute_HappyPath_CreatesPaymentAndSavesPendingPayment()
    {
        var (userRepo, currentUser, paymentProvider, useCase) = BuildHarness();
        var user = User.Register("ivan@example.com", "hash$hash$hash$hash").Value;
        user.StartTrial(DateTime.UtcNow, TimeSpan.FromDays(30));
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        paymentProvider
            .Setup(x => x.CreateAsync(
                user.Id,
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PaymentCreated, Error>(
                new PaymentCreated("pay-123", "https://yk/checkout/pay-123")));

        var result = await useCase.Execute(
            new CreatePaymentCommand(SubscriptionPlan.Monthly), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CheckoutUrl.Should().Be("https://yk/checkout/pay-123");
        result.Value.ExternalPaymentId.Should().Be("pay-123");
        user.Subscription.Status.Should().Be(SubscriptionStatus.PendingPayment);
        user.Subscription.LastPaymentId.Should().Be("pay-123");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_PaymentProviderFails_NoSaveCalled()
    {
        var (userRepo, currentUser, paymentProvider, useCase) = BuildHarness();
        var user = User.Register("ivan@example.com", "hash$hash$hash$hash").Value;
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        paymentProvider
            .Setup(x => x.CreateAsync(
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PaymentCreated, Error>(
                Errors.General.Failure("payment.failed", "Provider returned 500")));

        var result = await useCase.Execute(
            new CreatePaymentCommand(SubscriptionPlan.Monthly), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
        user.Subscription.Status.Should().Be(SubscriptionStatus.None);
    }

    [Fact]
    public async Task Execute_UserAlreadyActive_ReturnsAlreadyActive()
    {
        var (userRepo, currentUser, paymentProvider, useCase) = BuildHarness();
        var user = User.Register("ivan@example.com", "hash$hash$hash$hash").Value;
        user.ActivateSubscription(
            SubscriptionPlan.Monthly,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            "old-pay");
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        // Провайдер CreateAsync вызовется ДО RequestPayment (важно — мы
        // создаём платёж у YooKassa, потом мутируем юзера). Для теста
        // отдаём успех; AlreadyActive рождается в доменном методе.
        paymentProvider
            .Setup(x => x.CreateAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PaymentCreated, Error>(
                new PaymentCreated("pay-new", "https://yk/checkout/pay-new")));

        var result = await useCase.Execute(
            new CreatePaymentCommand(SubscriptionPlan.Monthly), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.already.active");
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<ICurrentUserService> CurrentUser,
        Mock<IPaymentProvider> PaymentProvider,
        CreatePaymentUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var paymentProvider = new Mock<IPaymentProvider>();
        var options = Options.Create(new SubscriptionOptions());
        var useCase = new CreatePaymentUseCase(
            userRepo.Object,
            currentUser.Object,
            paymentProvider.Object,
            options,
            TestExecutor.With<CreatePaymentCommand, CreatePaymentCommandValidator>());
        return (userRepo, currentUser, paymentProvider, useCase);
    }
}
