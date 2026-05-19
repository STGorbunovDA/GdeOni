using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.Validation;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты Memory CRUD use case'ов: AddMemory (любой авторизованный),
/// RemoveMemory (автор memory / автор карточки / admin). Сделано
/// в одном файле, потому что это смежная логика и общий setup.
/// </summary>
public sealed class MemoryUseCaseTests
{
    private static readonly Guid CardAuthorId = Guid.NewGuid();
    private static readonly Guid MemoryAuthorId = Guid.NewGuid();
    private static readonly Guid OutsiderId = Guid.NewGuid();

    /// <summary>
    /// AddMemory: любой авторизованный → memory создан со статусом
    /// Approved, AuthorUserId = currentUserId, Save вызван.
    /// D14: модерация воспоминаний отключена — use case сразу
    /// вызывает Approve() после AddMemory.
    /// </summary>
    [Fact]
    public async Task AddMemory_AnyAuthenticated_AddsAsApproved()
    {
        // Arrange
        var deceased = Deceased.Create(
            "Иван", "Иванов", null,
            null, new DateOnly(2010, 1, 1), null, CardAuthorId).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(MemoryAuthorId));
        deceasedRepo
            .Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new AddMemoryUseCase(
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<AddMemoryCommand, AddMemoryCommandValidator>());

        // Act
        var result = await useCase.Execute(
            new AddMemoryCommand(deceased.Id, "Хороший человек был"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        deceased.Memories.Should().HaveCount(1);
        var memory = deceased.Memories.Single();
        memory.Text.Should().Be("Хороший человек был");
        memory.AuthorUserId.Should().Be(MemoryAuthorId);
        memory.ModerationStatus.Should().Be(ModerationStatus.Approved);
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// RemoveMemory автором memory → success.
    /// </summary>
    [Fact]
    public async Task RemoveMemory_MemoryAuthor_SucceedsAndSaves()
        => await AssertRemoveMemoryScenario(
            currentUserId: MemoryAuthorId,
            isAdmin: false,
            shouldSucceed: true);

    /// <summary>
    /// RemoveMemory автором карточки (модерация) → success.
    /// </summary>
    [Fact]
    public async Task RemoveMemory_CardAuthor_SucceedsAndSaves()
        => await AssertRemoveMemoryScenario(
            currentUserId: CardAuthorId,
            isAdmin: false,
            shouldSucceed: true);

    /// <summary>
    /// RemoveMemory админом → success.
    /// </summary>
    [Fact]
    public async Task RemoveMemory_Admin_SucceedsAndSaves()
        => await AssertRemoveMemoryScenario(
            currentUserId: OutsiderId,
            isAdmin: true,
            shouldSucceed: true);

    /// <summary>
    /// RemoveMemory outsider → DeleteMemoryForbidden.
    /// Save не вызывается, memory не удалена.
    /// </summary>
    [Fact]
    public async Task RemoveMemory_Outsider_ReturnsDeleteForbidden()
        => await AssertRemoveMemoryScenario(
            currentUserId: OutsiderId,
            isAdmin: false,
            shouldSucceed: false,
            expectedErrorCode: "deceased_memory.delete.forbidden");

    private static async Task AssertRemoveMemoryScenario(
        Guid currentUserId,
        bool isAdmin,
        bool shouldSucceed,
        string? expectedErrorCode = null)
    {
        // Arrange: aggregate + memory автором MemoryAuthorId.
        var deceased = Deceased.Create(
            "Иван", "Иванов", null,
            null, new DateOnly(2010, 1, 1), null, CardAuthorId).Value;
        var memory = deceased.AddMemory("Текст", MemoryAuthorId).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));
        currentUser.Setup(x => x.IsAdmin()).Returns(isAdmin);
        deceasedRepo
            .Setup(x => x.GetByIdWithMemoryById(deceased.Id, memory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new RemoveMemoryUseCase(
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<RemoveMemoryCommand, RemoveMemoryCommandValidator>());

        // Act
        var result = await useCase.Execute(
            new RemoveMemoryCommand(deceased.Id, memory.Id),
            CancellationToken.None);

        // Assert
        if (shouldSucceed)
        {
            result.IsSuccess.Should().BeTrue();
            deceased.Memories.Should().BeEmpty();
            deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(expectedErrorCode);
            deceased.Memories.Should().HaveCount(1);
            deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
