using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.Model;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.Validation;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты Memory (Update/Reject) + Metadata (Update/Clear).
/// </summary>
public sealed class MemoryAndMetadataUseCaseTests
{
    private static readonly Guid CardAuthorId = Guid.NewGuid();
    private static readonly Guid MemoryAuthorId = Guid.NewGuid();
    private static readonly Guid OutsiderId = Guid.NewGuid();

    /// <summary>
    /// UpdateMemory автором memory → success, текст обновлён, статус
    /// сброшен в Pending (anti-bypass через EditMemory).
    /// </summary>
    [Fact]
    public async Task UpdateMemory_Author_ResetsToPendingAndSaves()
    {
        var deceased = MakeDeceased();
        var memory = deceased.AddMemory("Старый текст", MemoryAuthorId).Value;
        deceased.ApproveMemory(memory.Id);

        var (deceasedRepo, currentUser) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(MemoryAuthorId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo.Setup(x => x.GetByIdWithMemoryById(deceased.Id, memory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UpdateMemoryUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<UpdateMemoryCommand, UpdateMemoryCommandValidator>());

        var result = await useCase.Execute(
            new UpdateMemoryCommand(deceased.Id, memory.Id, "Новый текст"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        memory.Text.Should().Be("Новый текст");
        memory.ModerationStatus.Should().Be(ModerationStatus.Pending);
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UpdateMemory outsider → UpdateMemoryForbidden.
    /// </summary>
    [Fact]
    public async Task UpdateMemory_Outsider_ReturnsForbidden()
    {
        var deceased = MakeDeceased();
        var memory = deceased.AddMemory("Текст", MemoryAuthorId).Value;

        var (deceasedRepo, currentUser) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(OutsiderId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo.Setup(x => x.GetByIdWithMemoryById(deceased.Id, memory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UpdateMemoryUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<UpdateMemoryCommand, UpdateMemoryCommandValidator>());

        var result = await useCase.Execute(
            new UpdateMemoryCommand(deceased.Id, memory.Id, "хакнуто"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_memory.author.forbidden");
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// RejectMemory не-admin → RejectMemoryForbidden.
    /// </summary>
    [Fact]
    public async Task RejectMemory_NotAdmin_ReturnsForbidden()
    {
        var (deceasedRepo, currentUser) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(OutsiderId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        var useCase = new RejectMemoryUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<RejectMemoryCommand, RejectMemoryCommandValidator>());

        var result = await useCase.Execute(
            new RejectMemoryCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_memory_reject.verify.forbidden");
    }

    /// <summary>
    /// RejectMemory admin happy: ModerationStatus=Rejected, Save.
    /// </summary>
    [Fact]
    public async Task RejectMemory_Admin_SetsRejectedAndSaves()
    {
        var deceased = MakeDeceased();
        var memory = deceased.AddMemory("Текст", MemoryAuthorId).Value;

        var (deceasedRepo, currentUser) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(OutsiderId));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo.Setup(x => x.GetByIdWithMemoryById(deceased.Id, memory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new RejectMemoryUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<RejectMemoryCommand, RejectMemoryCommandValidator>());

        var result = await useCase.Execute(
            new RejectMemoryCommand(deceased.Id, memory.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        memory.ModerationStatus.Should().Be(ModerationStatus.Rejected);
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UpdateMetadata автор → success, метаданные обновлены.
    /// </summary>
    [Fact]
    public async Task UpdateMetadata_Author_Saves()
    {
        var deceased = MakeDeceased();

        var (deceasedRepo, currentUser) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CardAuthorId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UpdateMetadataUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<UpdateMetadataCommand, UpdateMetadataCommandValidator>());

        var result = await useCase.Execute(
            new UpdateMetadataCommand(deceased.Id, "Эпитафия", null, null, false, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceased.Metadata.Epitaph.Should().Be("Эпитафия");
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UpdateMetadata outsider → UpdateDeceasedMetadataForbidden.
    /// </summary>
    [Fact]
    public async Task UpdateMetadata_Outsider_ReturnsForbidden()
    {
        var deceased = MakeDeceased();

        var (deceasedRepo, currentUser) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(OutsiderId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UpdateMetadataUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<UpdateMetadataCommand, UpdateMetadataCommandValidator>());

        var result = await useCase.Execute(
            new UpdateMetadataCommand(deceased.Id, "x", null, null, false, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_metadata.update.forbidden");
    }

    /// <summary>
    /// ClearMetadata автор → метаданные пустые, Save.
    /// </summary>
    [Fact]
    public async Task ClearMetadata_Author_ClearsAndSaves()
    {
        var deceased = MakeDeceased();
        var metadata = DeceasedMetadata.Create("Старая эпитафия", null, null, false, null).Value;
        deceased.UpdateMetadata(metadata);

        var (deceasedRepo, currentUser) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CardAuthorId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new ClearMetadataUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<ClearMetadataCommand, ClearMetadataCommandValidator>());

        var result = await useCase.Execute(
            new ClearMetadataCommand(deceased.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceased.Metadata.IsEmpty().Should().BeTrue();
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// ClearMetadata outsider → DeleteDeceasedMetadataForbidden.
    /// </summary>
    [Fact]
    public async Task ClearMetadata_Outsider_ReturnsForbidden()
    {
        var deceased = MakeDeceased();

        var (deceasedRepo, currentUser) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(OutsiderId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new ClearMetadataUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<ClearMetadataCommand, ClearMetadataCommandValidator>());

        var result = await useCase.Execute(
            new ClearMetadataCommand(deceased.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_metadata.author.forbidden");
    }

    private static Deceased MakeDeceased() =>
        Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, CardAuthorId).Value;

    private static (Mock<IDeceasedRepository> Repo, Mock<ICurrentUserService> CurrentUser) BuildMocks()
    {
        return (new Mock<IDeceasedRepository>(), new Mock<ICurrentUserService>());
    }
}
