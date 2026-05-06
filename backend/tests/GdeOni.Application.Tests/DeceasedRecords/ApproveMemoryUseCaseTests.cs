using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты <see cref="ApproveMemoryUseCase"/> — admin-only модерация
/// воспоминаний. Покрываем: non-admin → 403, admin happy → 200,
/// повторный Approve → 409 (AlreadyApproved).
/// </summary>
public sealed class ApproveMemoryUseCaseTests
{
    /// <summary>
    /// Не-админ → ApproveMemoryForbidden.
    /// До GetByIdWithMemoryById даже не доходит — модерация
    /// чужих воспоминаний строго admin-only.
    /// </summary>
    [Fact]
    public async Task Execute_NonAdmin_ReturnsApproveMemoryForbidden()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        var useCase = new ApproveMemoryUseCase(
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<ApproveMemoryCommand, ApproveMemoryCommandValidator>());

        var result = await useCase.Execute(
            new ApproveMemoryCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_memory_approve.verify.forbidden");
        deceasedRepo.Verify(
            x => x.GetByIdWithMemoryById(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Admin happy path: memory переведён в Approved, Save вызван.
    /// </summary>
    [Fact]
    public async Task Execute_AdminFreshMemory_ApprovesAndSaves()
    {
        // Arrange
        var deceased = Deceased.Create(
            "Иван", "Иванов", null,
            null, new DateOnly(2010, 1, 1), null, Guid.NewGuid()).Value;
        var memory = deceased.AddMemory("Текст", Guid.NewGuid()).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo
            .Setup(x => x.GetByIdWithMemoryById(deceased.Id, memory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new ApproveMemoryUseCase(
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<ApproveMemoryCommand, ApproveMemoryCommandValidator>());

        // Act
        var result = await useCase.Execute(
            new ApproveMemoryCommand(deceased.Id, memory.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        memory.ModerationStatus.Should().Be(ModerationStatus.Approved);
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Повторный Approve → AlreadyApproved (409). Domain-инвариант,
    /// use case прокидывает.
    /// </summary>
    [Fact]
    public async Task Execute_AdminAlreadyApprovedMemory_ReturnsAlreadyApproved()
    {
        var deceased = Deceased.Create(
            "Иван", "Иванов", null,
            null, new DateOnly(2010, 1, 1), null, Guid.NewGuid()).Value;
        var memory = deceased.AddMemory("Текст", Guid.NewGuid()).Value;
        deceased.ApproveMemory(memory.Id); // pre-approved

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo
            .Setup(x => x.GetByIdWithMemoryById(deceased.Id, memory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new ApproveMemoryUseCase(
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<ApproveMemoryCommand, ApproveMemoryCommandValidator>());

        var result = await useCase.Execute(
            new ApproveMemoryCommand(deceased.Id, memory.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_memory.already.approved");
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }
}
