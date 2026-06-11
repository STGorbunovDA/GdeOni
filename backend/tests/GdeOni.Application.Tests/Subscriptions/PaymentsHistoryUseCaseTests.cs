using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Queries.GetAdminPayments.Model;
using GdeOni.Application.Subscriptions.Queries.GetAdminPayments.UseCase;
using GdeOni.Application.Subscriptions.Queries.GetAdminPayments.Validation;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.UseCase;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.Validation;
using GdeOni.Application.Tests.TestSupport;
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
        var useCase = new GetMyPaymentsUseCase(
            paymentRepo.Object, currentUser.Object,
            TestExecutor.With<GetMyPaymentsQuery, GetMyPaymentsQueryValidator>());

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

        var useCase = new GetMyPaymentsUseCase(
            paymentRepo.Object, currentUser.Object,
            TestExecutor.With<GetMyPaymentsQuery, GetMyPaymentsQueryValidator>());

        var result = await useCase.Execute(new GetMyPaymentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ExternalPaymentId.Should().Be("pay-x");
        result.Value.Items[0].Status.Should().Be("Pending");
    }

    /// <summary>
    /// Раньше use case делал silent clamping (Page=-5 → 1). Теперь
    /// валидатор отдаёт 400 — клиент узнаёт о своей ошибке вместо
    /// "почему я не вижу никаких платежей".
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task GetMy_InvalidPage_ReturnsValidationError(int badPage)
    {
        var paymentRepo = new Mock<ISubscriptionPaymentRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));

        var useCase = new GetMyPaymentsUseCase(
            paymentRepo.Object, currentUser.Object,
            TestExecutor.With<GetMyPaymentsQuery, GetMyPaymentsQueryValidator>());

        var result = await useCase.Execute(new GetMyPaymentsQuery(Page: badPage), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        paymentRepo.Verify(x => x.GetPagedForUser(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public async Task GetMy_InvalidPageSize_ReturnsValidationError(int badSize)
    {
        var paymentRepo = new Mock<ISubscriptionPaymentRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));

        var useCase = new GetMyPaymentsUseCase(
            paymentRepo.Object, currentUser.Object,
            TestExecutor.With<GetMyPaymentsQuery, GetMyPaymentsQueryValidator>());

        var result = await useCase.Execute(new GetMyPaymentsQuery(PageSize: badSize), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetAdmin_NotAdmin_ReturnsForbidden()
    {
        var paymentRepo = new Mock<ISubscriptionPaymentRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        var useCase = new GetAdminPaymentsUseCase(
            paymentRepo.Object, currentUser.Object,
            TestExecutor.With<GetAdminPaymentsQuery, GetAdminPaymentsQueryValidator>());

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

        var useCase = new GetAdminPaymentsUseCase(
            paymentRepo.Object, currentUser.Object,
            TestExecutor.With<GetAdminPaymentsQuery, GetAdminPaymentsQueryValidator>());

        var result = await useCase.Execute(new GetAdminPaymentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].UserEmail.Should().Be("user@example.com");
        result.Value.Items[0].ExternalPaymentId.Should().Be("pay-admin");
    }
}
