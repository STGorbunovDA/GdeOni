using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Notifications;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.Create.Model;
using GdeOni.Application.Support.Commands.Create.UseCase;
using GdeOni.Application.Support.Commands.Create.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Support;

/// <summary>
/// D25. Тесты Create Manual-тикета: Add + Save вызваны, severity =
/// Normal (юзер не может выставить Urgent сам), unauthorized пропускает
/// до failure.
/// </summary>
public sealed class CreateSupportTicketUseCaseTests
{
    [Fact]
    public async Task Execute_HappyPath_AddsAndSaves()
    {
        var (repo, currentUser, useCase) = BuildHarness();
        var userId = Guid.NewGuid();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId));

        var result = await useCase.Execute(
            new CreateSupportTicketCommand(
                SupportTicketKind.Payment,
                "Не приходит подтверждение",
                "Оплатил, статус не меняется"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.Verify(
            x => x.Add(It.Is<SupportTicket>(t =>
                t.UserId == userId
                && t.Source == SupportTicketSource.Manual
                && t.Severity == SupportTicketSeverity.Normal
                && t.Status == SupportTicketStatus.Open),
                It.IsAny<CancellationToken>()),
            Times.Once);
        repo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_Unauthorized_ReturnsError()
    {
        var (repo, currentUser, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Failure<Guid, Error>(Errors.General.Unauthorized()));

        var result = await useCase.Execute(
            new CreateSupportTicketCommand(SupportTicketKind.Bug, "T", "D"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        repo.Verify(
            x => x.Add(It.IsAny<SupportTicket>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static (
        Mock<ISupportTicketRepository> Repo,
        Mock<ICurrentUserService> CurrentUser,
        CreateSupportTicketUseCase UseCase) BuildHarness()
    {
        var repo = new Mock<ISupportTicketRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        // Moq по умолчанию возвращает Task.CompletedTask для async-методов —
        // фан-аут уведомления в тесте просто no-op.
        var notifications = new Mock<INotificationService>();
        var useCase = new CreateSupportTicketUseCase(
            repo.Object,
            currentUser.Object,
            TestExecutor.With<CreateSupportTicketCommand, CreateSupportTicketCommandValidator>(),
            notifications.Object,
            TimeProvider.System);
        return (repo, currentUser, useCase);
    }
}
