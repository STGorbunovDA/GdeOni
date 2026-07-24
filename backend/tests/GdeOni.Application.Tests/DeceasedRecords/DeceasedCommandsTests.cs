using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Application.DeceasedRecords.Commands.Create.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Create.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Delete.Model;
using GdeOni.Application.DeceasedRecords.Commands.Delete.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Delete.Validation;
using GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.Model;
using GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.Model;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Update.Model;
using GdeOni.Application.DeceasedRecords.Commands.Update.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Update.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты Create / Update / SetBurialFromGps / Delete / Unverified —
/// командные use case'ы Deceased aggregate. Один файл, общий setup.
/// </summary>
public sealed class DeceasedCommandsTests
{
    private static readonly Guid CreatorId = Guid.NewGuid();

    /// <summary>
    /// Create happy: ExistsBySearchKey=false, Add+Save вызваны.
    /// </summary>
    [Fact]
    public async Task Create_Happy_Saves()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CreatorId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo.Setup(x => x.ExistsById(CreatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        deceasedRepo.Setup(x => x.ExistsBySearchKey(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new CreateDeceasedUseCase(
            deceasedRepo.Object, currentUser.Object, userRepo.Object,
            TestExecutor.With<CreateDeceasedCommand>(
                new CreateDeceasedCommandValidator(TimeProvider.System)));

        var result = await useCase.Execute(
            new CreateDeceasedCommand(
                "Иван", "Иванов", null,
                BirthDate: null,
                DeathDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
                ShortDescription: null,
                Biography: null,
                BurialLocation: null,
                Memories: null,
                Metadata: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceasedRepo.Verify(
            x => x.Add(It.IsAny<Deceased>(), It.IsAny<CancellationToken>()),
            Times.Once);
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Create дубликат SearchKey → AlreadyExists. Add не вызывается.
    /// </summary>
    [Fact]
    public async Task Create_DuplicateSearchKey_ReturnsAlreadyExists()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CreatorId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo.Setup(x => x.ExistsById(CreatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        deceasedRepo.Setup(x => x.ExistsBySearchKey(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateDeceasedUseCase(
            deceasedRepo.Object, currentUser.Object, userRepo.Object,
            TestExecutor.With<CreateDeceasedCommand>(
                new CreateDeceasedCommandValidator(TimeProvider.System)));

        var result = await useCase.Execute(
            new CreateDeceasedCommand(
                "Иван", "Иванов", null, null,
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
                null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.already.exists");
        deceasedRepo.Verify(
            x => x.Add(It.IsAny<Deceased>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Update outsider → UpdateForbidden.
    /// </summary>
    [Fact]
    public async Task Update_Outsider_ReturnsUpdateForbidden()
    {
        var deceased = Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, CreatorId).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UpdateDeceasedUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<UpdateDeceasedCommand>(
                new UpdateDeceasedCommandValidator(TimeProvider.System)));

        var result = await useCase.Execute(
            new UpdateDeceasedCommand(
                deceased.Id, "Пётр", "Петров", null, null,
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)),
                null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.update.forbidden");
    }

    /// <summary>
    /// SetBurialLocationFromGps outsider → SetBurialLocationForbidden.
    /// </summary>
    [Fact]
    public async Task SetBurialFromGps_Outsider_ReturnsForbidden()
    {
        var deceased = Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, CreatorId).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new SetBurialLocationFromGpsUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<SetBurialLocationFromGpsCommand, SetBurialLocationFromGpsCommandValidator>());

        var result = await useCase.Execute(
            new SetBurialLocationFromGpsCommand(deceased.Id, 50.0, 30.0, 5.0),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.burial_location.set.forbidden");
    }

    /// <summary>
    /// SetBurialLocationFromGps автор → success + Save.
    /// </summary>
    [Fact]
    public async Task SetBurialFromGps_Author_Saves()
    {
        var deceased = Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, CreatorId).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CreatorId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new SetBurialLocationFromGpsUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<SetBurialLocationFromGpsCommand, SetBurialLocationFromGpsCommandValidator>());

        var result = await useCase.Execute(
            new SetBurialLocationFromGpsCommand(deceased.Id, 50.0, 30.0, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceased.BurialLocation.Should().NotBeNull();
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Delete не-admin → DeleteForbidden.
    /// </summary>
    [Fact]
    public async Task Delete_NotAdmin_ReturnsDeleteForbidden()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var fileStorage = new Mock<IFileStorage>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CreatorId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        var useCase = new DeleteDeceasedUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<DeleteDeceasedCommand, DeleteDeceasedCommandValidator>(),
            NullLogger<DeleteDeceasedUseCase>.Instance);

        var result = await useCase.Execute(
            new DeleteDeceasedCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.delete.forbidden");
    }

    /// <summary>
    /// Delete admin happy: каскадный DeleteById + DeleteAsync для каждого media-файла.
    /// </summary>
    [Fact]
    public async Task Delete_Admin_DeletesAndCleansUpFiles()
    {
        var deceased = Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, CreatorId).Value;
        deceased.AddMedia(Guid.NewGuid(), MediaKind.DeceasedPhoto,
            "photo.jpg", "deceased-photos", "key1",
            "image/jpeg", 1000);

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var fileStorage = new Mock<IFileStorage>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo.Setup(x => x.GetByIdWithMedia(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new DeleteDeceasedUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<DeleteDeceasedCommand, DeleteDeceasedCommandValidator>(),
            NullLogger<DeleteDeceasedUseCase>.Instance);

        var result = await useCase.Execute(
            new DeleteDeceasedCommand(deceased.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceasedRepo.Verify(x => x.DeleteById(deceased.Id, It.IsAny<CancellationToken>()), Times.Once);
        fileStorage.Verify(
            x => x.DeleteAsync("deceased-photos", "key1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Unverified не-admin → UnverifiedForbidden.
    /// </summary>
    [Fact]
    public async Task Unverified_NotAdmin_ReturnsForbidden()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CreatorId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        var useCase = new UnverifiedDeceasedUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<UnverifiedDeceasedCommand, UnverifiedDeceasedCommandValidator>());

        var result = await useCase.Execute(
            new UnverifiedDeceasedCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.unverify.forbidden");
    }

    /// <summary>
    /// Unverified admin happy: deceased.Verify() уже сделан, потом
    /// Unverified снимает флаг.
    /// </summary>
    [Fact]
    public async Task Unverified_Admin_FlipsFlag()
    {
        var deceased = Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, CreatorId).Value;
        deceased.Verify();

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UnverifiedDeceasedUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<UnverifiedDeceasedCommand, UnverifiedDeceasedCommandValidator>());

        var result = await useCase.Execute(
            new UnverifiedDeceasedCommand(deceased.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceased.IsVerified.Should().BeFalse();
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Unverified admin на не-verified → NotVerified.
    /// </summary>
    [Fact]
    public async Task Unverified_AlreadyNotVerified_ReturnsNotVerified()
    {
        var deceased = Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, CreatorId).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UnverifiedDeceasedUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<UnverifiedDeceasedCommand, UnverifiedDeceasedCommandValidator>());

        var result = await useCase.Execute(
            new UnverifiedDeceasedCommand(deceased.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.not.verified");
    }
}
