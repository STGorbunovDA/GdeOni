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
    /// UpdateMainInfo меняет имя/период/описание + Touch + RebuildSearchKey.
    /// Главное — после изменения имени SearchKey пересчитывается, иначе
    /// уникальный индекс ux_deceased_search_key перестанет ловить
    /// дубликаты после переименования.
    /// </summary>
    [Fact]
    public void UpdateMainInfo_NewName_RebuildsSearchKey()
    {
        var deceased = CreateSampleDeceased();
        var oldSearchKey = deceased.SearchKey;

        var result = deceased.UpdateMainInfo(
            firstName: "Пётр",
            lastName: "Петров",
            middleName: null,
            birthDate: null,
            deathDate: SampleDeathDate,
            shortDescription: "Новое описание",
            biography: null);

        result.IsSuccess.Should().BeTrue();
        deceased.Name.FirstName.Should().Be("Пётр");
        deceased.Name.LastName.Should().Be("Петров");
        deceased.SearchKey.Should().NotBe(oldSearchKey);
        deceased.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// D11.10.2: PUT с теми же значениями (включая ShortDescription/
    /// Biography = null/null как у sample) — no-op, UpdatedAtUtc не
    /// двигается, SearchKey не пересобирается.
    /// </summary>
    [Fact]
    public void UpdateMainInfo_SameValues_DoesNotTouch()
    {
        var deceased = CreateSampleDeceased();
        var oldSearchKey = deceased.SearchKey;
        var oldUpdatedAt = deceased.UpdatedAtUtc;

        var result = deceased.UpdateMainInfo(
            firstName: "Иван",
            lastName: "Иванов",
            middleName: null,
            birthDate: new DateOnly(1950, 6, 15),
            deathDate: SampleDeathDate,
            shortDescription: null,
            biography: null);

        result.IsSuccess.Should().BeTrue();
        deceased.SearchKey.Should().Be(oldSearchKey);
        deceased.UpdatedAtUtc.Should().Be(oldUpdatedAt);
    }

    /// <summary>
    /// D11.14.1: UpdateMetadata с тем же metadata (по
    /// GetEqualityComponents) — no-op, UpdatedAtUtc не двигается.
    /// </summary>
    [Fact]
    public void UpdateMetadata_SameValues_DoesNotTouch()
    {
        var deceased = CreateSampleDeceased();
        var first = DeceasedMetadata.Create(
            "Покойся с миром", "Православие", null, false, null).Value;
        deceased.UpdateMetadata(first);
        var initialUpdatedAt = deceased.UpdatedAtUtc;

        var same = DeceasedMetadata.Create(
            "Покойся с миром", "Православие", null, false, null).Value;
        var result = deceased.UpdateMetadata(same);

        result.IsSuccess.Should().BeTrue();
        deceased.UpdatedAtUtc.Should().Be(initialUpdatedAt);
    }

    /// <summary>
    /// D11.14.2: ClearMetadata на уже Empty metadata — no-op,
    /// UpdatedAtUtc не двигается. Свежесозданная карточка имеет
    /// Metadata = Empty по умолчанию.
    /// </summary>
    [Fact]
    public void ClearMetadata_AlreadyEmpty_DoesNotTouch()
    {
        var deceased = CreateSampleDeceased();
        deceased.UpdatedAtUtc.Should().BeNull();

        var result = deceased.ClearMetadata();

        result.IsSuccess.Should().BeTrue();
        deceased.UpdatedAtUtc.Should().BeNull();
    }

    /// <summary>
    /// AddMemory — happy path: запись добавлена в коллекцию,
    /// статус Pending (модерация ещё не решила), AuthorUserId сохранён.
    /// </summary>
    [Fact]
    public void AddMemory_ValidText_AppendsAsPending()
    {
        var deceased = CreateSampleDeceased();
        var authorId = Guid.NewGuid();

        var result = deceased.AddMemory("Хороший человек был", authorId);

        result.IsSuccess.Should().BeTrue();
        deceased.Memories.Should().HaveCount(1);
        deceased.Memories.Single().Text.Should().Be("Хороший человек был");
        deceased.Memories.Single().AuthorUserId.Should().Be(authorId);
        deceased.Memories.Single().ModerationStatus.Should().Be(ModerationStatus.Pending);
    }

    /// <summary>
    /// EditMemory меняет текст у существующей memory; на несуществующей
    /// — NotFound. Это аналог Domain-level "404" для коллекций.
    /// </summary>
    [Fact]
    public void EditMemory_NonExistentId_ReturnsNotFound()
    {
        var deceased = CreateSampleDeceased();

        var result = deceased.EditMemory(Guid.NewGuid(), "Новый текст");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_memory.not.found");
    }

    /// <summary>
    /// RemoveMemory: existing → success, non-existent → NotFound.
    /// </summary>
    [Fact]
    public void RemoveMemory_Existing_RemovesAndTouches()
    {
        var deceased = CreateSampleDeceased();
        var memory = deceased.AddMemory("Текст", Guid.NewGuid()).Value;
        var initialUpdatedAt = deceased.UpdatedAtUtc;

        var result = deceased.RemoveMemory(memory.Id);

        result.IsSuccess.Should().BeTrue();
        deceased.Memories.Should().BeEmpty();
        deceased.UpdatedAtUtc.Should().NotBe(initialUpdatedAt);
    }

    [Fact]
    public void RemoveMemory_NonExistent_ReturnsNotFound()
    {
        var deceased = CreateSampleDeceased();

        var result = deceased.RemoveMemory(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_memory.not.found");
    }

    /// <summary>
    /// ApproveMemory / RejectMemory: успех + повтор → AlreadyApproved /
    /// AlreadyRejected (хвост из DeceasedMemoryEntry, но идущий через
    /// агрегат — проверяем что aggregate.Touch() тоже срабатывает).
    /// </summary>
    [Fact]
    public void ApproveMemory_Existing_SucceedsAndTouchesAggregate()
    {
        var deceased = CreateSampleDeceased();
        var memory = deceased.AddMemory("Текст").Value;
        var beforeApprove = deceased.UpdatedAtUtc;

        var result = deceased.ApproveMemory(memory.Id);

        result.IsSuccess.Should().BeTrue();
        memory.ModerationStatus.Should().Be(ModerationStatus.Approved);
        deceased.UpdatedAtUtc.Should().NotBe(beforeApprove);
    }

    /// <summary>
    /// Verify / Unverify: успех + повтор → AlreadyVerified / NotVerified
    /// (это Conflict, а не Validation — деление на категории важно
    /// для правильного маппинга на 409).
    /// </summary>
    [Fact]
    public void Verify_FreshDeceased_SetsIsVerifiedTrue()
    {
        var deceased = CreateSampleDeceased();

        var result = deceased.Verify();

        result.IsSuccess.Should().BeTrue();
        deceased.IsVerified.Should().BeTrue();
    }

    [Fact]
    public void Verify_AlreadyVerified_ReturnsAlreadyVerified()
    {
        var deceased = CreateSampleDeceased();
        deceased.Verify();

        var result = deceased.Verify();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.already.verified");
    }

    [Fact]
    public void Unverify_NotVerified_ReturnsNotVerified()
    {
        var deceased = CreateSampleDeceased();

        var result = deceased.Unverify();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.not.verified");
    }

    /// <summary>
    /// RemoveMedia при MainMediaId == removed: MainMediaId должен
    /// обнулиться, иначе у агрегата остаётся "висящая" ссылка
    /// на удалённое media — баг #1 кандидат для прода.
    /// </summary>
    [Fact]
    public void RemoveMedia_WhenItIsMain_ClearsMainMediaId()
    {
        // Arrange: добавили + апрувнули + назначили main.
        var deceased = CreateSampleDeceased();
        var media = AddApprovedMainPhoto(deceased);
        deceased.MainMediaId.Should().Be(media.Id);

        // Act: удалили этот media.
        var result = deceased.RemoveMedia(media.Id);

        // Assert: MainMediaId/MainMedia очищены.
        result.IsSuccess.Should().BeTrue();
        deceased.MainMediaId.Should().BeNull();
        deceased.MainMedia.Should().BeNull();
    }

    /// <summary>
    /// SetMainPhoto на новое фото снимает IsMainPhoto со старого main.
    /// Без этого мы получаем два IsMainPhoto=true в коллекции — UI
    /// показывает оба или случайное.
    /// </summary>
    [Fact]
    public void SetMainPhoto_NewMain_UnsetsOldMainFlag()
    {
        // Arrange: добавили два DeceasedPhoto, оба апрувнуты, первое — main.
        var deceased = CreateSampleDeceased();
        var firstMedia = AddApprovedMainPhoto(deceased);
        var secondMedia = deceased.AddMedia(
            uploadedByUserId: Guid.NewGuid(),
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo2.jpg",
            bucket: "deceased-photos",
            storageKey: "deceased-photos/" + Guid.NewGuid() + ".jpg",
            contentType: "image/jpeg",
            sizeBytes: 2048).Value;
        deceased.ApproveMedia(secondMedia.Id);

        // Act: переключаем main на secondMedia.
        var result = deceased.SetMainPhoto(secondMedia.Id);

        // Assert: первое больше не main, второе — да; MainMediaId обновлён.
        result.IsSuccess.Should().BeTrue();
        firstMedia.IsMainPhoto.Should().BeFalse();
        secondMedia.IsMainPhoto.Should().BeTrue();
        deceased.MainMediaId.Should().Be(secondMedia.Id);
    }

    /// <summary>
    /// RejectMedia на main-фото: статус Rejected, IsMainPhoto обнулён,
    /// MainMediaId на агрегате обнулён. Это сценарий "загрузил
    /// фото → стало main → модератор отклонил" — карточка не должна
    /// продолжать показывать его.
    /// </summary>
    [Fact]
    public void RejectMedia_WhenItIsMain_ClearsMainOnAggregate()
    {
        var deceased = CreateSampleDeceased();
        var media = AddApprovedMainPhoto(deceased);

        var result = deceased.RejectMedia(media.Id);

        result.IsSuccess.Should().BeTrue();
        media.ModerationStatus.Should().Be(ModerationStatus.Rejected);
        media.IsMainPhoto.Should().BeFalse();
        deceased.MainMediaId.Should().BeNull();
    }

    /// <summary>
    /// ApproveMedia / RejectMedia на уже Approved/Rejected → AlreadyX.
    /// Покрываем хвост Domain.DeceasedMedia.Approve тестов через
    /// aggregate-метод (важно, что aggregate тоже не делает Touch
    /// для no-op случая? — пока domain делает Touch на возврате
    /// failure. Проверяем именно код ошибки).
    /// </summary>
    [Fact]
    public void ApproveMedia_AlreadyApproved_ReturnsAlreadyApproved()
    {
        var deceased = CreateSampleDeceased();
        var media = deceased.AddMedia(
            Guid.NewGuid(), MediaKind.DeceasedPhoto,
            "photo.jpg", "deceased-photos", "deceased-photos/x.jpg",
            "image/jpeg", 1024).Value;
        deceased.ApproveMedia(media.Id);

        var result = deceased.ApproveMedia(media.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_media.already.approved");
    }

    /// <summary>
    /// UpdateMediaDescription happy: описание обновляется на media,
    /// Touch проставляется на агрегате (для UpdatedAtUtc на Deceased).
    /// </summary>
    [Fact]
    public void UpdateMediaDescription_Existing_UpdatesAndTouchesAggregate()
    {
        var deceased = CreateSampleDeceased();
        var media = deceased.AddMedia(
            Guid.NewGuid(), MediaKind.DeceasedPhoto,
            "photo.jpg", "deceased-photos", "deceased-photos/x.jpg",
            "image/jpeg", 1024).Value;
        var beforeUpdate = deceased.UpdatedAtUtc;

        var result = deceased.UpdateMediaDescription(media.Id, "Новое описание");

        result.IsSuccess.Should().BeTrue();
        media.Description.Should().Be("Новое описание");
        deceased.UpdatedAtUtc.Should().NotBe(beforeUpdate);
    }

    /// <summary>
    /// GetMainPhoto возвращает фото только если оно Approved.
    /// Pending main (теоретически возможно если SetMainPhoto обошёл
    /// проверку) — должно скрываться от публичного URL карточки.
    /// </summary>
    [Fact]
    public void GetMainPhoto_OnlyApproved_ReturnsOnlyApprovedAsMain()
    {
        var deceased = CreateSampleDeceased();
        var media = AddApprovedMainPhoto(deceased);

        // Approved — отдаётся.
        deceased.GetMainPhoto().Should().Be(media);

        // Симулируем downgrade в Pending через RejectMedia → Reject
        // меняет IsMainPhoto, и SetMainPhoto не пройдёт повторно
        // на Pending — проверяем именно сценарий "после reject
        // GetMainPhoto возвращает null".
        deceased.RejectMedia(media.Id);
        deceased.GetMainPhoto().Should().BeNull();
    }

    /// <summary>
    /// SearchKey детерминирован: одинаковые имена в разных регистрах
    /// дают одинаковый ключ (NormalizeString делает Trim+ToUpperInvariant).
    /// Без этого "иванов" и "ИВАНОВ" считались бы разными личностями
    /// и индекс ux_deceased_search_key пропускал бы дубликаты.
    /// </summary>
    [Fact]
    public void SearchKey_DifferentCase_ProducesIdenticalKey()
    {
        var lower = Deceased.Create(
            "иван", "иванов", "иванович",
            new DateOnly(1950, 6, 15), new DateOnly(2010, 1, 1),
            null, Guid.NewGuid()).Value;
        var upper = Deceased.Create(
            "ИВАН", "ИВАНОВ", "ИВАНОВИЧ",
            new DateOnly(1950, 6, 15), new DateOnly(2010, 1, 1),
            null, Guid.NewGuid()).Value;

        lower.SearchKey.Should().Be(upper.SearchKey);
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

    /// <summary>
    /// Helper: добавляет DeceasedPhoto, апрувит, назначает main.
    /// Возвращает media — чтобы тесты могли обращаться к его Id.
    /// </summary>
    private static DeceasedMedia AddApprovedMainPhoto(Deceased deceased)
    {
        var media = deceased.AddMedia(
            uploadedByUserId: Guid.NewGuid(),
            kind: MediaKind.DeceasedPhoto,
            originalFileName: "photo.jpg",
            bucket: "deceased-photos",
            storageKey: "deceased-photos/" + Guid.NewGuid() + ".jpg",
            contentType: "image/jpeg",
            sizeBytes: 1024).Value;
        deceased.ApproveMedia(media.Id);
        deceased.SetMainPhoto(media.Id);
        return media;
    }
}
