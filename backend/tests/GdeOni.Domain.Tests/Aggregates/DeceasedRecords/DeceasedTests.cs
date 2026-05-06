using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Tests.Aggregates.DeceasedRecords;

/// <summary>
/// Тесты агрегата <see cref="Deceased"/> — корня доменной модели
/// карточки умершего. Покрывает: создание (с BurialLocation и без),
/// смену места захоронения, добавление media, назначение главного фото.
/// Все мутации идут через доменные методы, возвращающие Result/UnitResult.
/// </summary>
public sealed class DeceasedTests
{
    private static readonly Guid SampleUserId = Guid.NewGuid();
    private static readonly DateOnly SampleDeathDate = new(2010, 6, 14);

    /// <summary>
    /// BurialLocation — опциональная часть карточки. Сценарий:
    /// пользователь создаёт карточку из общего списка (не у могилы),
    /// без координат. Карточка должна успешно создаться, BurialLocation
    /// — null, SearchKey — рассчитан без burial-данных. Это нужно для
    /// /create endpoint'а в отличие от /at-grave.
    /// </summary>
    [Fact]
    public void Create_WithoutBurialLocation_BuildsAggregate()
    {
        // Act: создаём карточку без burialLocation.
        var result = Deceased.Create(
            firstName: "Иван",
            lastName: "Иванов",
            middleName: null,
            birthDate: new DateOnly(1950, 6, 15),
            deathDate: SampleDeathDate,
            burialLocation: null,
            createdByUserId: SampleUserId);

        // Assert: успех, координаты не заданы, имя/период собраны
        // из VO; SearchKey построен (даже без burial — там идут "-").
        result.IsSuccess.Should().BeTrue();
        result.Value.BurialLocation.Should().BeNull();
        result.Value.Name.FirstName.Should().Be("Иван");
        result.Value.Name.LastName.Should().Be("Иванов");
        result.Value.LifePeriod.DeathDate.Should().Be(SampleDeathDate);
        result.Value.SearchKey.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// CreatedByUserId обязателен — каждая карточка должна знать
    /// своего автора (используется в правах доступа к редактированию,
    /// удалению фото и т.д.). Guid.Empty — это "не передал" с точки
    /// зрения domain'а.
    /// </summary>
    [Fact]
    public void Create_EmptyCreatedByUserId_ReturnsCreatedByRequired()
    {
        var result = Deceased.Create(
            firstName: "Иван",
            lastName: "Иванов",
            middleName: null,
            birthDate: null,
            deathDate: SampleDeathDate,
            burialLocation: null,
            createdByUserId: Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.created_by.required");
    }

    /// <summary>
    /// ChangeBurialLocation должен корректно работать в обе стороны:
    /// null → значение (поставили координаты позже) и значение → null
    /// (очистили). Здесь покрываем переход null → значение, потому
    /// что это главный сценарий "нашли могилу позже регистрации".
    /// Также проверяем, что после смены пересчитывается SearchKey
    /// (туда входят CemeteryName/City/PlotNumber/GraveNumber) и
    /// проставляется UpdatedAtUtc.
    /// </summary>
    [Fact]
    public void ChangeBurialLocation_FromNullToValue_UpdatesLocationAndSearchKey()
    {
        // Arrange: карточка без burial.
        var deceased = Deceased.Create(
            firstName: "Иван",
            lastName: "Иванов",
            middleName: null,
            birthDate: null,
            deathDate: SampleDeathDate,
            burialLocation: null,
            createdByUserId: SampleUserId).Value;

        var initialSearchKey = deceased.SearchKey;
        var newLocation = BurialLocation.Create(
            latitude: 55.7558,
            longitude: 37.6173,
            cemeteryName: "Ваганьковское").Value;

        // Act: ставим координаты + кладбище.
        var result = deceased.ChangeBurialLocation(newLocation);

        // Assert: успех + BurialLocation теперь не null + SearchKey
        // изменился (в нём учтено CemeteryName) + UpdatedAtUtc проставлен.
        result.IsSuccess.Should().BeTrue();
        deceased.BurialLocation.Should().Be(newLocation);
        deceased.SearchKey.Should().NotBe(initialSearchKey);
        deceased.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// AddMedia добавляет элемент в коллекцию _media и проставляет
    /// его параметры. Главное, что проверяем — что коллекция Media
    /// (read-only обёртка) возвращает добавленный элемент с теми же
    /// атрибутами, и что Touch проставил UpdatedAtUtc.
    /// </summary>
    [Fact]
    public void AddMedia_ValidParameters_AppendsToMediaCollection()
    {
        // Arrange
        var deceased = CreateSampleDeceased();
        var uploaderId = Guid.NewGuid();

        // Act: добавляем фото умершего (DeceasedPhoto).
        var result = deceased.AddMedia(
            uploadedByUserId: uploaderId,
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo.jpg",
            bucket: "deceased-photos",
            storageKey: "deceased-photos/" + Guid.NewGuid() + ".jpg",
            contentType: "image/jpeg",
            sizeBytes: 1024);

        // Assert: media создалось, попало в коллекцию (1 элемент),
        // у созданного элемента — переданный uploader.
        result.IsSuccess.Should().BeTrue();
        deceased.Media.Should().HaveCount(1);
        deceased.Media.Single().UploadedByUserId.Should().Be(uploaderId);
        deceased.Media.Single().Kind.Should().Be(MediaKind.DeceasedPhoto);
        deceased.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// SetMainPhoto имеет два инварианта:
    /// (1) только media с Kind=DeceasedPhoto может быть главным фото;
    /// (2) только Approved фото может быть главным.
    /// Здесь проверяем (1): пытаемся назначить главным GravePhoto —
    /// должно отвергнуться с конкретным error-кодом, чтобы клиент
    /// понял "это не фото человека, это фото могилы".
    /// </summary>
    [Fact]
    public void SetMainPhoto_NotDeceasedPhotoKind_ReturnsOnlyDeceasedPhotoCanBeMain()
    {
        // Arrange: добавляем GravePhoto, не DeceasedPhoto.
        var deceased = CreateSampleDeceased();
        var media = deceased.AddMedia(
            uploadedByUserId: Guid.NewGuid(),
            kind: MediaKind.GravePhoto,
            originalFileName: "grave.jpg",
            bucket: "grave-photos",
            storageKey: "grave-photos/" + Guid.NewGuid() + ".jpg",
            contentType: "image/jpeg",
            sizeBytes: 2048).Value;

        // Act: пытаемся сделать его главным.
        var result = deceased.SetMainPhoto(media.Id);

        // Assert: отказ с кодом про "только DeceasedPhoto".
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.main_photo.only_deceased_photo");
    }

    /// <summary>
    /// SetMainPhoto, инвариант (2): только Approved фото может быть
    /// главным. Свежезагруженное фото имеет статус Pending — пока
    /// модератор не одобрил, юзер не может его опубликовать как
    /// "лицо карточки". Защищает от обхода модерации.
    /// </summary>
    [Fact]
    public void SetMainPhoto_PendingPhoto_ReturnsMainPhotoMustBeApproved()
    {
        // Arrange: DeceasedPhoto Kind, но статус по умолчанию — Pending.
        var deceased = CreateSampleDeceased();
        var media = deceased.AddMedia(
            uploadedByUserId: Guid.NewGuid(),
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo.jpg",
            bucket: "deceased-photos",
            storageKey: "deceased-photos/" + Guid.NewGuid() + ".jpg",
            contentType: "image/jpeg",
            sizeBytes: 1024).Value;

        // Act
        var result = deceased.SetMainPhoto(media.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.main_photo.not_approved");
    }

    /// <summary>
    /// SetMainPhoto happy path: DeceasedPhoto + Approved → становится
    /// главным, MainMediaId агрегата = id media, IsMainPhoto на самом
    /// media = true.
    /// </summary>
    [Fact]
    public void SetMainPhoto_ApprovedDeceasedPhoto_SetsMainMediaId()
    {
        // Arrange: добавили + апрувнули фото.
        var deceased = CreateSampleDeceased();
        var media = deceased.AddMedia(
            uploadedByUserId: Guid.NewGuid(),
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo.jpg",
            bucket: "deceased-photos",
            storageKey: "deceased-photos/" + Guid.NewGuid() + ".jpg",
            contentType: "image/jpeg",
            sizeBytes: 1024).Value;
        deceased.ApproveMedia(media.Id);

        // Act
        var result = deceased.SetMainPhoto(media.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        deceased.MainMediaId.Should().Be(media.Id);
        media.IsMainPhoto.Should().BeTrue();
    }

    /// <summary>
    /// Helper для создания минимальной валидной карточки —
    /// чтобы не дублировать boilerplate в каждом тесте.
    /// </summary>
    private static Deceased CreateSampleDeceased()
    {
        return Deceased.Create(
            firstName: "Иван",
            lastName: "Иванов",
            middleName: null,
            birthDate: new DateOnly(1950, 6, 15),
            deathDate: SampleDeathDate,
            burialLocation: null,
            createdByUserId: SampleUserId).Value;
    }
}
