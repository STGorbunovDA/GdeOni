using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMediaModeration.Model;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMediaModeration.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMediaModeration.Validation;
using GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.Model;
using GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.Validation;
using GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UploadMedia.Model;
using GdeOni.Application.DeceasedRecords.Commands.UploadMedia.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UploadMedia.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaById.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaById.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaById.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaList.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaList.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaList.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты Media use case'ов: Upload (валидаторы и rollback при Save-fail),
/// GetList (фильтр Pending для не-владельцев), GetById (presigned URL
/// для документов), SetMainPhoto, Approve/Reject moderation.
/// </summary>
public sealed class MediaUseCaseTests
{
    private static readonly Guid CardAuthorId = Guid.NewGuid();
    private static readonly Guid OutsiderId = Guid.NewGuid();

    /// <summary>
    /// UploadMedia с неправильным MIME → PhotoContentTypeNotAllowed.
    /// FileStorage.UploadAsync не вызывается.
    /// </summary>
    [Fact]
    public async Task Upload_InvalidMime_ReturnsContentTypeNotAllowed()
    {
        var (deceasedRepo, fileStorage, currentUser, _) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CardAuthorId));

        var useCase = new UploadMediaUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<UploadMediaCommand, UploadMediaCommandValidator>(),
            NullLogger<UploadMediaUseCase>.Instance);

        var result = await useCase.Execute(new UploadMediaCommand
        {
            DeceasedId = Guid.NewGuid(),
            Kind = FileKind.DeceasedPhoto,
            OriginalFileName = "file.txt",
            ContentType = "text/plain",
            SizeBytes = 100,
            Content = new MemoryStream(new byte[] { 1, 2, 3 })
        }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.photo.content_type.not_allowed");
        fileStorage.Verify(
            x => x.UploadAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// UploadMedia слишком большой файл → PhotoTooLarge.
    /// </summary>
    [Fact]
    public async Task Upload_TooLargePhoto_ReturnsPhotoTooLarge()
    {
        var (deceasedRepo, fileStorage, currentUser, _) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CardAuthorId));

        var useCase = new UploadMediaUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<UploadMediaCommand, UploadMediaCommandValidator>(),
            NullLogger<UploadMediaUseCase>.Instance);

        var result = await useCase.Execute(new UploadMediaCommand
        {
            DeceasedId = Guid.NewGuid(),
            Kind = FileKind.DeceasedPhoto,
            OriginalFileName = "big.jpg",
            ContentType = "image/jpeg",
            SizeBytes = FileValidator.MaxPhotoSizeBytes + 1,
            Content = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })
        }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.photo.too_large");
    }

    /// <summary>
    /// UploadMedia magic bytes mismatch (объявил image/jpeg, а реально
    /// PNG-сигнатура) → MagicBytesMismatch.
    /// </summary>
    [Fact]
    public async Task Upload_MagicBytesMismatch_ReturnsMismatch()
    {
        var (deceasedRepo, fileStorage, currentUser, _) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CardAuthorId));

        var useCase = new UploadMediaUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<UploadMediaCommand, UploadMediaCommandValidator>(),
            NullLogger<UploadMediaUseCase>.Instance);

        // PNG signature вместо JPEG.
        var content = new MemoryStream(new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0, 0, 0, 0
        });
        var result = await useCase.Execute(new UploadMediaCommand
        {
            DeceasedId = Guid.NewGuid(),
            Kind = FileKind.DeceasedPhoto,
            OriginalFileName = "fake.jpg",
            ContentType = "image/jpeg",
            SizeBytes = content.Length,
            Content = content
        }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.content.magic_bytes_mismatch");
    }

    /// <summary>
    /// UploadMedia: Save упал после Upload → DeleteAsync best-effort
    /// rollback'ит файл. Save throw'ает → use case бросает дальше.
    /// </summary>
    [Fact]
    public async Task Upload_SaveFails_DeletesFileFromStorageAndRethrows()
    {
        var deceased = MakeDeceased();

        var (deceasedRepo, fileStorage, currentUser, _) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CardAuthorId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);
        fileStorage
            .Setup(x => x.UploadAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFile(
                "deceased-photos", "key1", "image/jpeg", 16, "photo.jpg"));
        deceasedRepo
            .Setup(x => x.Save(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var useCase = new UploadMediaUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<UploadMediaCommand, UploadMediaCommandValidator>(),
            NullLogger<UploadMediaUseCase>.Instance);

        var act = () => useCase.Execute(new UploadMediaCommand
        {
            DeceasedId = deceased.Id,
            Kind = FileKind.DeceasedPhoto,
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 16,
            Content = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        fileStorage.Verify(
            x => x.DeleteAsync("deceased-photos", "key1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// GetMediaList не-владелец → moderationStatus принудительно Approved
    /// независимо от запрошенного. Pending/Rejected чужой карточки
    /// не утекают в ответ.
    /// </summary>
    [Fact]
    public async Task GetList_NonOwner_GetsApprovedOnly()
    {
        var deceased = MakeDeceased();

        var (deceasedRepo, fileStorage, currentUser, _) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(OutsiderId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo
            .Setup(x => x.GetByIdReadOnly(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);
        deceasedRepo
            .Setup(x => x.GetMediaPaged(
                deceased.Id,
                It.IsAny<MediaKind?>(),
                ModerationStatus.Approved,
                1, 10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DeceasedMedia>(), 0));

        var useCase = new GetMediaListUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetMediaListQuery, GetMediaListQueryValidator>());

        var result = await useCase.Execute(
            new GetMediaListQuery(deceased.Id, null, ModerationStatus.Pending, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Repository вызван именно с ModerationStatus.Approved — outsider
        // не может увидеть Pending даже если попросил.
        deceasedRepo.Verify(
            x => x.GetMediaPaged(
                deceased.Id, null, ModerationStatus.Approved,
                1, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// GetMediaById для Document → Url присылается как presigned
    /// (IsPresigned=true). GetPresignedUrlAsync вызывается, GetPublicUrl нет.
    /// </summary>
    [Fact]
    public async Task GetById_Document_ReturnsPresignedUrl()
    {
        var deceased = MakeDeceased();
        var media = deceased.AddMedia(
            CardAuthorId, MediaKind.Document,
            "doc.pdf", "deceased-documents", "k1",
            "application/pdf", 1000).Value;

        var (deceasedRepo, fileStorage, currentUser, _) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CardAuthorId));
        deceasedRepo
            .Setup(x => x.GetByIdWithMediaById(deceased.Id, media.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);
        fileStorage
            .Setup(x => x.GetPresignedUrlAsync(
                "deceased-documents", "k1", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://signed.example/url");

        var useCase = new GetMediaByIdUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetMediaByIdQuery, GetMediaByIdQueryValidator>());

        var result = await useCase.Execute(
            new GetMediaByIdQuery(deceased.Id, media.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be("https://signed.example/url");
        result.Value.IsPresigned.Should().BeTrue();
        fileStorage.Verify(
            x => x.GetPublicUrl(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    /// GetMediaById для DeceasedPhoto → public URL (IsPresigned=false).
    /// </summary>
    [Fact]
    public async Task GetById_Photo_ReturnsPublicUrl()
    {
        var deceased = MakeDeceased();
        var media = deceased.AddMedia(
            CardAuthorId, MediaKind.DeceasedPhoto,
            "p.jpg", "deceased-photos", "k1",
            "image/jpeg", 1000).Value;

        var (deceasedRepo, fileStorage, currentUser, _) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CardAuthorId));
        deceasedRepo
            .Setup(x => x.GetByIdWithMediaById(deceased.Id, media.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);
        fileStorage
            .Setup(x => x.GetPublicUrl("deceased-photos", "k1"))
            .Returns("http://public/url");

        var useCase = new GetMediaByIdUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<GetMediaByIdQuery, GetMediaByIdQueryValidator>());

        var result = await useCase.Execute(
            new GetMediaByIdQuery(deceased.Id, media.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be("http://public/url");
        result.Value.IsPresigned.Should().BeFalse();
    }

    /// <summary>
    /// SetMainPhoto outsider → SetMainPhotoForbidden. Save не вызывается.
    /// </summary>
    [Fact]
    public async Task SetMainPhoto_Outsider_ReturnsForbidden()
    {
        var deceased = MakeDeceased();

        var (deceasedRepo, _, currentUser, _) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(OutsiderId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo
            .Setup(x => x.GetByIdWithMedia(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new SetMainMediaPhotoUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<SetMainMediaPhotoCommand, SetMainMediaPhotoCommandValidator>());

        var result = await useCase.Execute(
            new SetMainMediaPhotoCommand(deceased.Id, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.main_photo.forbidden");
    }

    /// <summary>
    /// ApproveMediaModeration не-admin → ModerationForbidden.
    /// </summary>
    [Fact]
    public async Task ApproveMedia_NotAdmin_ReturnsModerationForbidden()
    {
        var (deceasedRepo, _, currentUser, _) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(OutsiderId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        var useCase = new ApproveMediaModerationUseCase(
            deceasedRepo.Object, currentUser.Object,
            TestExecutor.With<ApproveMediaModerationCommand, ApproveMediaModerationCommandValidator>());

        var result = await useCase.Execute(
            new ApproveMediaModerationCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.moderation.forbidden");
    }

    /// <summary>
    /// RejectMediaModeration admin: media отклонено + DeleteAsync вызван
    /// (best-effort удаление файла из MinIO).
    /// </summary>
    [Fact]
    public async Task RejectMedia_Admin_DeletesFileAndSaves()
    {
        var deceased = MakeDeceased();
        var media = deceased.AddMedia(
            CardAuthorId, MediaKind.DeceasedPhoto,
            "p.jpg", "deceased-photos", "k1",
            "image/jpeg", 1000).Value;

        var (deceasedRepo, fileStorage, currentUser, _) = BuildMocks();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo
            .Setup(x => x.GetByIdWithMediaById(deceased.Id, media.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new RejectMediaModerationUseCase(
            deceasedRepo.Object, fileStorage.Object, currentUser.Object,
            TestExecutor.With<RejectMediaModerationCommand, RejectMediaModerationCommandValidator>(),
            NullLogger<RejectMediaModerationUseCase>.Instance);

        var result = await useCase.Execute(
            new RejectMediaModerationCommand(deceased.Id, media.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        media.ModerationStatus.Should().Be(ModerationStatus.Rejected);
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        fileStorage.Verify(
            x => x.DeleteAsync("deceased-photos", "k1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Deceased MakeDeceased() =>
        Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, CardAuthorId).Value;

    private static (
        Mock<IDeceasedRepository> Repo,
        Mock<IFileStorage> FileStorage,
        Mock<ICurrentUserService> CurrentUser,
        Mock<IUserRepository> UserRepo) BuildMocks()
    {
        return (
            new Mock<IDeceasedRepository>(),
            new Mock<IFileStorage>(),
            new Mock<ICurrentUserService>(),
            new Mock<IUserRepository>());
    }
}
