using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Tests.Aggregates.DeceasedRecords;

/// <summary>
/// Тесты <see cref="DeceasedMedia"/> — entity-элемент коллекции
/// Media у Deceased. Хранит метаданные файла (bucket, storageKey,
/// contentType, sizeBytes), статус модерации, флаг IsMainPhoto.
/// Конструкторы приватные — все мутации через Create + доменные методы.
/// </summary>
public sealed class DeceasedMediaTests
{
    private static readonly Guid SampleDeceasedId = Guid.NewGuid();
    private static readonly Guid SampleUploaderId = Guid.NewGuid();

    /// <summary>
    /// DeceasedId == Guid.Empty — defensive проверка для случая,
    /// если кто-то вызывает Create напрямую (минуя Deceased.AddMedia,
    /// который заведомо передаёт правильный Id).
    /// </summary>
    [Fact]
    public void Create_EmptyDeceasedId_ReturnsDeceasedIdRequired()
    {
        var result = DeceasedMedia.Create(
            deceasedId: Guid.Empty,
            uploadedByUserId: SampleUploaderId,
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo.jpg",
            bucket: "deceased-photos",
            storageKey: "deceased-photos/x.jpg",
            contentType: "image/jpeg",
            sizeBytes: 1024);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.deceased_id.required");
    }

    /// <summary>
    /// UploadedByUserId == Guid.Empty — каждый файл должен иметь
    /// автора (используется для прав DeleteMedia).
    /// </summary>
    [Fact]
    public void Create_EmptyUploadedByUserId_ReturnsUploadedByRequired()
    {
        var result = DeceasedMedia.Create(
            deceasedId: SampleDeceasedId,
            uploadedByUserId: Guid.Empty,
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo.jpg",
            bucket: "deceased-photos",
            storageKey: "deceased-photos/x.jpg",
            contentType: "image/jpeg",
            sizeBytes: 1024);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.uploaded_by.required");
    }

    /// <summary>
    /// SizeBytes <= 0 — пустой файл или повреждённый upload.
    /// На уровне API такое тоже не пропустят, но domain — последний
    /// рубеж.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositiveSizeBytes_ReturnsSizeBytesInvalid(long sizeBytes)
    {
        var result = DeceasedMedia.Create(
            deceasedId: SampleDeceasedId,
            uploadedByUserId: SampleUploaderId,
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo.jpg",
            bucket: "deceased-photos",
            storageKey: "deceased-photos/x.jpg",
            contentType: "image/jpeg",
            sizeBytes: sizeBytes);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.size_bytes.invalid");
    }

    /// <summary>
    /// Каждое из обязательных текстовых полей — отдельный required-код.
    /// Покрываем все четыре одной theory: OriginalFileName, Bucket,
    /// StorageKey, ContentType.
    /// </summary>
    [Theory]
    [InlineData("originalFileName", "deceased_media.original_file_name.required")]
    [InlineData("bucket", "deceased_media.bucket.required")]
    [InlineData("storageKey", "deceased_media.storage_key.required")]
    [InlineData("contentType", "deceased_media.content_type.required")]
    public void Create_EmptyRequiredField_ReturnsCorrectRequiredError(string field, string expectedCode)
    {
        var result = DeceasedMedia.Create(
            deceasedId: SampleDeceasedId,
            uploadedByUserId: SampleUploaderId,
            kind: MediaKind.DeceasedPhoto,
            originalFileName: field == "originalFileName" ? "" : "photo.jpg",
            bucket: field == "bucket" ? "" : "deceased-photos",
            storageKey: field == "storageKey" ? "" : "deceased-photos/x.jpg",
            contentType: field == "contentType" ? "" : "image/jpeg",
            sizeBytes: 1024);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
    }

    /// <summary>
    /// Description длиннее MaxDescriptionLength → DescriptionTooLong.
    /// Description опционален при Create, проверка работает только
    /// если значение задано.
    /// </summary>
    [Fact]
    public void Create_DescriptionTooLong_ReturnsDescriptionTooLong()
    {
        var description = new string('а', DeceasedMedia.MaxDescriptionLength + 1);

        var result = DeceasedMedia.Create(
            deceasedId: SampleDeceasedId,
            uploadedByUserId: SampleUploaderId,
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo.jpg",
            bucket: "deceased-photos",
            storageKey: "deceased-photos/x.jpg",
            contentType: "image/jpeg",
            sizeBytes: 1024,
            description: description);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.description.too_long");
    }

    /// <summary>
    /// UpdateDescription с длинным текстом → DescriptionTooLong.
    /// Тот же Normalize, что в Create, но через отдельный метод.
    /// </summary>
    [Fact]
    public void UpdateDescription_TooLong_ReturnsDescriptionTooLong()
    {
        var media = CreateSampleMedia();
        var longDescription = new string('а', DeceasedMedia.MaxDescriptionLength + 1);

        var result = media.UpdateDescription(longDescription);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.description.too_long");
    }

    /// <summary>
    /// UpdateDescription с null / whitespace очищает Description
    /// (NormalizeDescription возвращает Success(null) для empty input).
    /// </summary>
    [Fact]
    public void UpdateDescription_EmptyValue_ClearsDescription()
    {
        var media = CreateSampleMedia(description: "Старое описание");

        var result = media.UpdateDescription("   ");

        result.IsSuccess.Should().BeTrue();
        media.Description.Should().BeNull();
    }

    /// <summary>
    /// Reject обнуляет IsMainPhoto: если медиа было main и
    /// модерация его отклонила, оно перестаёт быть main даже на
    /// уровне самого entity'а (плюс Deceased.RejectMedia обнулит
    /// MainMediaId — это покрыто в DeceasedTests).
    /// </summary>
    [Fact]
    public void Reject_MainPhoto_ClearsIsMainPhotoFlag()
    {
        // Arrange: media стало main (через MarkAsMainPhoto, который
        // internal — но мы можем дойти до этого статуса через
        // полный путь Deceased.SetMainPhoto. Здесь короче — создаём
        // media и вручную ставим IsMainPhoto через тот же internal API
        // он недоступен напрямую, поэтому делаем через Deceased.
        var deceased = Deceased.Create(
            "Иван", "Иванов", null,
            null, new DateOnly(2010, 1, 1), null, Guid.NewGuid()).Value;
        var media = deceased.AddMedia(
            SampleUploaderId, MediaKind.DeceasedPhoto,
            "photo.jpg", "deceased-photos", "deceased-photos/x.jpg",
            "image/jpeg", 1024).Value;
        deceased.ApproveMedia(media.Id);
        deceased.SetMainPhoto(media.Id);
        media.IsMainPhoto.Should().BeTrue();

        // Act: модерация отклонила.
        var result = media.Reject();

        // Assert: IsMainPhoto обнуляется.
        result.IsSuccess.Should().BeTrue();
        media.IsMainPhoto.Should().BeFalse();
        media.ModerationStatus.Should().Be(ModerationStatus.Rejected);
    }

    private static DeceasedMedia CreateSampleMedia(string? description = null)
    {
        // Через Deceased.AddMedia, потому что Create требует валидных
        // ID и т.д. — а entity-конструктор приватный.
        var deceased = Deceased.Create(
            "Иван", "Иванов", null,
            null, new DateOnly(2010, 1, 1), null, Guid.NewGuid()).Value;
        return deceased.AddMedia(
            SampleUploaderId, MediaKind.DeceasedPhoto,
            "photo.jpg", "deceased-photos", "deceased-photos/x.jpg",
            "image/jpeg", 1024, description).Value;
    }
}
