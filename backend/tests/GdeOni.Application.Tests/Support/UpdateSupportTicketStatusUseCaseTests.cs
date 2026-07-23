using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.UpdateStatus.Model;
using GdeOni.Application.Support.Commands.UpdateStatus.UseCase;
using GdeOni.Application.Support.Commands.UpdateStatus.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Support;

public sealed class UpdateSupportTicketStatusUseCaseTests
{
    [Fact]
    public async Task Execute_NotSuperAdmin_ReturnsForbidden()
    {
        var (_, currentUser, _, useCase) = BuildHarness();
        currentUser.Setup(x => x.IsSuperAdmin()).Returns(false);

        var result = await useCase.Execute(
            new UpdateSupportTicketStatusCommand(
                Guid.NewGuid(), SupportTicketStatus.InProgress, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Execute_TicketNotFound_ReturnsNotFound()
    {
        var (repo, currentUser, _, useCase) = BuildHarness();
        currentUser.Setup(x => x.IsSuperAdmin()).Returns(true);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        repo.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupportTicket?)null);

        var result = await useCase.Execute(
            new UpdateSupportTicketStatusCommand(
                Guid.NewGuid(), SupportTicketStatus.InProgress, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Execute_ResolveWithoutNote_ReturnsValidationError()
    {
        var (repo, currentUser, _, useCase) = BuildHarness();
        currentUser.Setup(x => x.IsSuperAdmin()).Returns(true);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        var ticket = SupportTicket.CreateManual(
            Guid.NewGuid(), SupportTicketKind.Other, "T", "D", DateTime.UtcNow).Value;
        repo.Setup(x => x.GetById(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var result = await useCase.Execute(
            new UpdateSupportTicketStatusCommand(ticket.Id, SupportTicketStatus.Resolved, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("support_ticket.resolution_note.required");
        repo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ResolveWithNote_SavesAndFixesAdmin()
    {
        var (repo, currentUser, _, useCase) = BuildHarness();
        var adminId = Guid.NewGuid();
        currentUser.Setup(x => x.IsSuperAdmin()).Returns(true);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(adminId));
        var ticket = SupportTicket.CreateManual(
            Guid.NewGuid(), SupportTicketKind.Other, "T", "D", DateTime.UtcNow).Value;
        repo.Setup(x => x.GetById(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var result = await useCase.Execute(
            new UpdateSupportTicketStatusCommand(
                ticket.Id, SupportTicketStatus.Resolved, "Выдан compli"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.Resolved);
        ticket.ResolvedByUserId.Should().Be(adminId);
        ticket.ResolutionNote.Should().Be("Выдан compli");
        repo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (
        Mock<ISupportTicketRepository> Repo,
        Mock<ICurrentUserService> CurrentUser,
        Mock<TimeProvider> TimeProvider,
        UpdateSupportTicketStatusUseCase UseCase) BuildHarness()
    {
        var repo = new Mock<ISupportTicketRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var time = new Mock<TimeProvider>();
        var useCase = new UpdateSupportTicketStatusUseCase(
            repo.Object,
            currentUser.Object,
            TestExecutor.With<UpdateSupportTicketStatusCommand, UpdateSupportTicketStatusCommandValidator>(),
            System.TimeProvider.System);
        return (repo, currentUser, time, useCase);
    }
}
