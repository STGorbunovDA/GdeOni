using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.Model;
using GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты <see cref="DeleteMediaUseCase"/> — удаление медиафайла.
/// D26: удалить файл может только admin (выкладка/удаление/правка
/// медиа закрыты под админа целиком, чтобы убрать юр. риск от
/// пользовательских загрузок чужих фото/документов). Любой другой
/// актор — автор файла, автор карточки, outsider — получает 403 /
/// `deceased_media.delete.forbidden`.
/// </summary>
public sealed class DeleteMediaUseCaseTests
{
    private static readonly Guid CardAuthorId = Guid.NewGuid();
    private static readonly Guid FileUploaderId = Guid.NewGuid();
    private static readonly Guid OutsiderId = Guid.NewGuid();

    /// <summary>
    /// Сценарий безопасности: пользователь, который не является ни
    /// автором карточки, ни автором файла, ни админом, не может
    /// удалить media. Use case ловит это до RemoveMedia (доменный
    /// метод не вызывается) и возвращает Forbidden.
    /// </summary>
    [Fact]
    public async Task Execute_OutsiderTriesToDelete_ReturnsDeleteForbidden()
    {
        // Arrange: создаём настоящий Deceased + media через домен.
        // Конструкторы Deceased приватные, поэтому без настоящего
        // Create аггрегат не построить — а нам нужен реалистичный
        // объект с заполненными CreatedByUserId и UploadedByUserId.
        var deceased = Deceased.Create(
            firstName: "Иван",
            lastName: "Иванов",
            middleName: null,
            birthDate: null,
            deathDate: new DateOnly(2010, 1, 1),
            burialLocation: null,
            createdByUserId: CardAuthorId).Value;

        var media = deceased.AddMedia(
            uploadedByUserId: FileUploaderId,
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo.jpg",
            bucket: "deceased-photos",
            storageKey: "deceased-photos/" + Guid.NewGuid() + ".jpg",
            contentType: "image/jpeg",
            sizeBytes: 1024).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var fileStorage = new Mock<IFileStorage>();
        var currentUser = new Mock<ICurrentUserService>();

        // Текущий — outsider (не автор карточки, не автор файла, не admin).
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(OutsiderId));
        currentUser
            .Setup(x => x.IsAdmin())
            .Returns(false);

        deceasedRepo
            .Setup(x => x.GetByIdWithMediaById(deceased.Id, media.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new DeleteMediaUseCase(
            deceasedRepo.Object,
            fileStorage.Object,
            currentUser.Object,
            TestExecutor.With<DeleteMediaCommand, DeleteMediaCommandValidator>(),
            NullLogger<DeleteMediaUseCase>.Instance);

        // Act
        var result = await useCase.Execute(
            new DeleteMediaCommand(deceased.Id, media.Id),
            CancellationToken.None);

        // Assert: 403 / `deceased_media.delete.forbidden`. БД не
        // менялась (Save не вызывался) — изменения в change-tracker
        // не попадают в SaveChanges.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.delete.forbidden");
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
        fileStorage.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// D26. Автор карточки больше не может удалять media — права на
    /// удаление перешли только администраторам. Тест охраняет регрессию,
    /// при которой кто-то вернул бы "автору карточки можно" в guard.
    /// </summary>
    [Fact]
    public async Task Execute_CardAuthorNonAdmin_ReturnsDeleteForbidden()
    {
        var deceased = Deceased.Create(
            firstName: "Иван",
            lastName: "Иванов",
            middleName: null,
            birthDate: null,
            deathDate: new DateOnly(2010, 1, 1),
            burialLocation: null,
            createdByUserId: CardAuthorId).Value;

        var media = deceased.AddMedia(
            uploadedByUserId: FileUploaderId,
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo.jpg",
            bucket: "deceased-photos",
            storageKey: "deceased-photos/" + Guid.NewGuid() + ".jpg",
            contentType: "image/jpeg",
            sizeBytes: 1024).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var fileStorage = new Mock<IFileStorage>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CardAuthorId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        deceasedRepo
            .Setup(x => x.GetByIdWithMediaById(deceased.Id, media.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new DeleteMediaUseCase(
            deceasedRepo.Object,
            fileStorage.Object,
            currentUser.Object,
            TestExecutor.With<DeleteMediaCommand, DeleteMediaCommandValidator>(),
            NullLogger<DeleteMediaUseCase>.Instance);

        var result = await useCase.Execute(
            new DeleteMediaCommand(deceased.Id, media.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.delete.forbidden");
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
        fileStorage.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Happy path для админа: даже если он не автор файла и не автор
    /// карточки, IsAdmin() = true даёт право удалить. Use case делает
    /// два эффекта: Save (метаданные исчезают из БД) и DeleteAsync
    /// (файл из MinIO). DeleteAsync — best-effort: если упадёт,
    /// orphan cleanup потом подберёт.
    /// </summary>
    [Fact]
    public async Task Execute_AdminCanDeleteAnyone_RemovesFromStorage()
    {
        // Arrange
        var deceased = Deceased.Create(
            firstName: "Иван",
            lastName: "Иванов",
            middleName: null,
            birthDate: null,
            deathDate: new DateOnly(2010, 1, 1),
            burialLocation: null,
            createdByUserId: CardAuthorId).Value;

        var bucket = "deceased-photos";
        var storageKey = "deceased-photos/" + Guid.NewGuid() + ".jpg";
        var media = deceased.AddMedia(
            uploadedByUserId: FileUploaderId,
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo.jpg",
            bucket: bucket,
            storageKey: storageKey,
            contentType: "image/jpeg",
            sizeBytes: 1024).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var fileStorage = new Mock<IFileStorage>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(OutsiderId));
        currentUser
            .Setup(x => x.IsAdmin())
            .Returns(true); // ← ключевое отличие от предыдущего теста.

        deceasedRepo
            .Setup(x => x.GetByIdWithMediaById(deceased.Id, media.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new DeleteMediaUseCase(
            deceasedRepo.Object,
            fileStorage.Object,
            currentUser.Object,
            TestExecutor.With<DeleteMediaCommand, DeleteMediaCommandValidator>(),
            NullLogger<DeleteMediaUseCase>.Instance);

        // Act
        var result = await useCase.Execute(
            new DeleteMediaCommand(deceased.Id, media.Id),
            CancellationToken.None);

        // Assert: успех + Save (commit метаданных) + DeleteAsync
        // с правильными bucket / storageKey (file storage).
        result.IsSuccess.Should().BeTrue();
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        fileStorage.Verify(
            x => x.DeleteAsync(bucket, storageKey, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
