using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Queries.GetAdminPayments.Model;
using GdeOni.Application.Subscriptions.Queries.GetAdminPayments.UseCase;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.UseCase;
using GdeOni.Domain.Aggregates.Subscriptions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Subscriptions;

/// <summary>
/// D23. Тесты на GetMyPaymentsUseCase + GetAdminPaymentsUseCase.
/// </summary>
public sealed class PaymentsHistoryUseCaseTests
{
    [Fact]
    public async Task GetMy_Unauthorized_ReturnsError()
    {
        var paymentRepo = new Mock<ISubscriptionPaymentRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Errors.General.Unauthorized());
        var useCase = new GetMyPaymentsUseCase(paymentRepo.Object, currentUser.Object);

        var result = await useCase.Execute(new GetMyPaymentsQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task GetMy_HappyPath_MapsDomainToResponse()
    {
        var paymentRepo = new Mock<ISubscriptionPaymentRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var userId = Guid.NewGuid();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId));

        var payment = SubscriptionPayment.Create(
            userId, "pay-x", SubscriptionPlan.Monthly, 49m, "https://yk/x",
            new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc)).Value;
        paymentRepo
            .Setup(x => x.GetPagedForUser(userId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<SubscriptionPayment> { payment }, 1));

        var useCase = new GetMyPaymentsUseCase(paymentRepo.Object, currentUser.Object);

        var result = await useCase.Execute(new GetMyPaymentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ExternalPaymentId.Should().Be("pay-x");
        result.Value.Items[0].Status.Should().Be("Pending");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(2, 2)]
    public async Task GetMy_NormalizesPageNumber(int input, int normalized)
    {
        var paymentRepo = new Mock<ISubscriptionPaymentRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        paymentRepo
            .Setup(x => x.GetPagedForUser(It.IsAny<Guid>(), normalized, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<SubscriptionPayment>(), 0));

        var useCase = new GetMyPaymentsUseCase(paymentRepo.Object, currentUser.Object);

        var result = await useCase.Execute(new GetMyPaymentsQuery(Page: input), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        paymentRepo.Verify(x => x.GetPagedForUser(
            It.IsAny<Guid>(), normalized, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAdmin_NotAdmin_ReturnsForbidden()
    {
        var paymentRepo = new Mock<ISubscriptionPaymentRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        var useCase = new GetAdminPaymentsUseCase(paymentRepo.Object, currentUser.Object);

        var result = await useCase.Execute(new GetAdminPaymentsQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task GetAdmin_HappyPath_ReturnsItemsWithUserEmail()
    {
        var paymentRepo = new Mock<ISubscriptionPaymentRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.IsAdmin()).Returns(true);

        var userId = Guid.NewGuid();
        var payment = SubscriptionPayment.Create(
            userId, "pay-admin", SubscriptionPlan.Monthly, 49m, null,
            new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc)).Value;
        paymentRepo
            .Setup(x => x.GetPagedForAdmin(
                null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (new List<(SubscriptionPayment, string)> { (payment, "user@example.com") }, 1));

        var useCase = new GetAdminPaymentsUseCase(paymentRepo.Object, currentUser.Object);

        var result = await useCase.Execute(new GetAdminPaymentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].UserEmail.Should().Be("user@example.com");
        result.Value.Items[0].ExternalPaymentId.Should().Be("pay-admin");
    }
}
