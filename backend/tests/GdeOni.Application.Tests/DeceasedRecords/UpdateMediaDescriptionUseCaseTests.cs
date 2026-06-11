using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты <see cref="UpdateMediaDescriptionUseCase"/>.
/// D26: редактирование описания media разрешено только админам —
/// наряду с выкладкой и удалением. Автор файла и автор карточки
/// больше не имеют прав. Outsider тоже получает 403.
/// </summary>
public sealed class UpdateMediaDescriptionUseCaseTests
{
    private static readonly Guid CardAuthorId = Guid.NewGuid();
    private static readonly Guid FileUploaderId = Guid.NewGuid();
    private static readonly Guid OutsiderId = Guid.NewGuid();

    [Fact]
    public async Task Execute_FileAuthorNonAdmin_ReturnsForbidden()
        => await AssertRightsScenario(
            currentUserId: FileUploaderId,
            isAdmin: false,
            shouldSucceed: false,
            expectedErrorCode: "deceased_media.update_description.forbidden");

    [Fact]
    public async Task Execute_CardAuthorNonAdmin_ReturnsForbidden()
        => await AssertRightsScenario(
            currentUserId: CardAuthorId,
            isAdmin: false,
            shouldSucceed: false,
            expectedErrorCode: "deceased_media.update_description.forbidden");

    [Fact]
    public async Task Execute_Admin_Succeeds()
        => await AssertRightsScenario(
            currentUserId: OutsiderId,
            isAdmin: true,
            shouldSucceed: true);

    [Fact]
    public async Task Execute_Outsider_ReturnsForbidden()
        => await AssertRightsScenario(
            currentUserId: OutsiderId,
            isAdmin: false,
            shouldSucceed: false,
            expectedErrorCode: "deceased_media.update_description.forbidden");

    /// <summary>
    /// Параметризованный helper: создаёт настоящий Deceased + media,
    /// конфигурирует моки под переданные права, прогоняет use case.
    /// Альтернатива дублированию 4 одинаковых Arrange-блоков.
    /// </summary>
    private static async Task AssertRightsScenario(
        Guid currentUserId,
        bool isAdmin,
        bool shouldSucceed,
        string? expectedErrorCode = null)
    {
        // Arrange: настоящий aggregate + media — конструкторы приватные.
        var deceased = Deceased.Create(
            "Иван", "Иванов", null,
            null, new DateOnly(2010, 1, 1), null, CardAuthorId).Value;
        var media = deceased.AddMedia(
            FileUploaderId,
            MediaKind.DeceasedPhoto,
            "photo.jpg",
            "deceased-photos",
            "deceased-photos/x.jpg",
            "image/jpeg",
            sizeBytes: 1024).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));
        currentUser.Setup(x => x.IsAdmin()).Returns(isAdmin);
        deceasedRepo
            .Setup(x => x.GetByIdWithMediaById(deceased.Id, media.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UpdateMediaDescriptionUseCase(
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<UpdateMediaDescriptionCommand, UpdateMediaDescriptionCommandValidator>());

        // Act
        var result = await useCase.Execute(
            new UpdateMediaDescriptionCommand(deceased.Id, media.Id, "Новое описание"),
            CancellationToken.None);

        // Assert: либо happy + Description обновлено + Save вызван,
        // либо Forbidden + Save НЕ вызван.
        if (shouldSucceed)
        {
            result.IsSuccess.Should().BeTrue();
            media.Description.Should().Be("Новое описание");
            deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(expectedErrorCode);
            deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
