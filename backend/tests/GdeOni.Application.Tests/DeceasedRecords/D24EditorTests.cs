using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.UpdateBurialLocationByEditor.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateBurialLocationByEditor.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateBurialLocationByEditor.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMainInfoByEditor.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMainInfoByEditor.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMainInfoByEditor.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadataByEditor.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadataByEditor.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadataByEditor.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// D24. Тесты collaborative editing: CanEditDeceasedPolicy + три
/// by-editor use case'а (MainInfo / Metadata / BurialLocation) +
/// два query (GetDeceasedEdits / GetAllEdits). Аудит обнаружил, что
/// эта фича задеплоена в проде без unit-тестов — закрываем долг.
/// </summary>
public sealed class D24EditorTests
{
    private static readonly Guid CardAuthorId = Guid.NewGuid();

    // ─────────────────────────── CanEditDeceasedPolicy ───────────────────────────

    /// <summary>
    /// Админ может править любую карточку, даже не отслеживая её.
    /// Repository НЕ зовётся — без БД-запроса.
    /// </summary>
    [Fact]
    public async Task Policy_Admin_AllowsWithoutTracking()
    {
        var (userRepo, currentUser, policy) = BuildPolicy();
        currentUser.Setup(x => x.IsAdmin()).Returns(true);

        var result = await policy.CheckAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        userRepo.Verify(
            x => x.IsActivelyTracking(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Не-админ с активным трекингом — разрешено.
    /// </summary>
    [Fact]
    public async Task Policy_ActivelyTrackingUser_Allows()
    {
        var (userRepo, currentUser, policy) = BuildPolicy();
        var userId = Guid.NewGuid();
        var deceasedId = Guid.NewGuid();
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId));
        userRepo.Setup(x => x.IsActivelyTracking(userId, deceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await policy.CheckAsync(deceasedId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Не-админ без трекинга → NotEditor (403). Защита от вандализма
    /// со случайных аккаунтов.
    /// </summary>
    [Fact]
    public async Task Policy_NonTrackingUser_ReturnsNotEditor()
    {
        var (userRepo, currentUser, policy) = BuildPolicy();
        var userId = Guid.NewGuid();
        var deceasedId = Guid.NewGuid();
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId));
        userRepo.Setup(x => x.IsActivelyTracking(userId, deceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await policy.CheckAsync(deceasedId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_edit.editor.forbidden");
    }

    /// <summary>
    /// Если не получили id текущего юзера (auth-issue) → пробрасываем ошибку,
    /// не молчим. Админ-чек к этому моменту вернул false.
    /// </summary>
    [Fact]
    public async Task Policy_NoCurrentUserId_ReturnsError()
    {
        var (_, currentUser, policy) = BuildPolicy();
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Failure<Guid, Error>(Errors.General.Unauthorized()));

        var result = await policy.CheckAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.unauthorized");
    }

    // ─────────────────────────── UpdateMainInfoByEditor ───────────────────────────

    /// <summary>
    /// Policy запретила → use case возвращает forbidden, в БД не лезет.
    /// </summary>
    [Fact]
    public async Task UpdateMainInfo_PolicyForbids_ReturnsForbidden()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = MockCurrentUser();
        var policy = MockPolicy(allow: false);

        var useCase = new UpdateMainInfoByEditorUseCase(
            deceasedRepo.Object, currentUser.Object, policy.Object,
            TestExecutor.With<UpdateMainInfoByEditorCommand, UpdateMainInfoByEditorCommandValidator>());

        var result = await useCase.Execute(
            MakeMainInfoCmd(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_edit.editor.forbidden");
        // Save НЕ вызвался — карточка не загружалась.
        deceasedRepo.Verify(
            x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Карточка не найдена → NotFound.
    /// </summary>
    [Fact]
    public async Task UpdateMainInfo_DeceasedNotFound_ReturnsNotFound()
    {
        var deceasedId = Guid.NewGuid();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo.Setup(x => x.GetById(deceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Deceased?)null);

        var useCase = new UpdateMainInfoByEditorUseCase(
            deceasedRepo.Object, MockCurrentUser().Object, MockPolicy(allow: true).Object,
            TestExecutor.With<UpdateMainInfoByEditorCommand, UpdateMainInfoByEditorCommandValidator>());

        var result = await useCase.Execute(
            MakeMainInfoCmd(deceasedId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        // Errors.General.NotFound("deceased", id) формирует код по схеме
        // {entity}.not.found — поэтому "deceased.not.found".
        result.Error.Code.Should().Be("deceased.not.found");
    }

    /// <summary>
    /// Happy: Save вызван, агрегат обновлён, audit-запись положена (через
    /// доменный UpdateMainInfo с editorUserId).
    /// </summary>
    [Fact]
    public async Task UpdateMainInfo_Happy_SavesAndUpdatesAggregate()
    {
        var editorId = Guid.NewGuid();
        var deceased = MakeDeceased();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var currentUser = MockCurrentUser(editorId);

        var useCase = new UpdateMainInfoByEditorUseCase(
            deceasedRepo.Object, currentUser.Object, MockPolicy(allow: true).Object,
            TestExecutor.With<UpdateMainInfoByEditorCommand, UpdateMainInfoByEditorCommandValidator>());

        var cmd = new UpdateMainInfoByEditorCommand(
            deceased.Id, "Пётр", "Петров", null,
            BirthDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-50)),
            DeathDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ShortDescription: null, Biography: null);

        var result = await useCase.Execute(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceased.Name.FirstName.Should().Be("Пётр");
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────── UpdateMetadataByEditor ───────────────────────────

    /// <summary>
    /// Policy запретила → forbidden, в БД не лезет.
    /// </summary>
    [Fact]
    public async Task UpdateMetadata_PolicyForbids_ReturnsForbidden()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var useCase = new UpdateMetadataByEditorUseCase(
            deceasedRepo.Object, MockCurrentUser().Object, MockPolicy(allow: false).Object,
            TestExecutor.With<UpdateMetadataByEditorCommand, UpdateMetadataByEditorCommandValidator>());

        var result = await useCase.Execute(
            new UpdateMetadataByEditorCommand(Guid.NewGuid(), null, null, null, false, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_edit.editor.forbidden");
        deceasedRepo.Verify(
            x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Все поля пустые + IsMilitaryService=false → use case зовёт
    /// ClearMetadata (no-op на чистой карточке, но Save всё равно
    /// вызывается — это OK, тест проверяет ветку Clear).
    /// </summary>
    [Fact]
    public async Task UpdateMetadata_AllEmpty_CallsClearAndSaves()
    {
        var deceased = MakeDeceased();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UpdateMetadataByEditorUseCase(
            deceasedRepo.Object, MockCurrentUser().Object, MockPolicy(allow: true).Object,
            TestExecutor.With<UpdateMetadataByEditorCommand, UpdateMetadataByEditorCommandValidator>());

        var result = await useCase.Execute(
            new UpdateMetadataByEditorCommand(deceased.Id, null, null, null, false, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Happy: эпитафия задана → UpdateMetadata вызван, Save вызван.
    /// </summary>
    [Fact]
    public async Task UpdateMetadata_Happy_SavesNewMetadata()
    {
        var deceased = MakeDeceased();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UpdateMetadataByEditorUseCase(
            deceasedRepo.Object, MockCurrentUser().Object, MockPolicy(allow: true).Object,
            TestExecutor.With<UpdateMetadataByEditorCommand, UpdateMetadataByEditorCommandValidator>());

        var result = await useCase.Execute(
            new UpdateMetadataByEditorCommand(
                deceased.Id,
                Epitaph: "Спи спокойно",
                Religion: null, Source: null,
                IsMilitaryService: false,
                AdditionalInfo: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceased.Metadata.Epitaph.Should().Be("Спи спокойно");
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────── UpdateBurialLocationByEditor ───────────────────────────

    /// <summary>
    /// Policy запретила → forbidden.
    /// </summary>
    [Fact]
    public async Task UpdateBurialLocation_PolicyForbids_ReturnsForbidden()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var useCase = new UpdateBurialLocationByEditorUseCase(
            deceasedRepo.Object, MockCurrentUser().Object, MockPolicy(allow: false).Object,
            TestExecutor.With<UpdateBurialLocationByEditorCommand, UpdateBurialLocationByEditorCommandValidator>());

        var result = await useCase.Execute(
            MakeBurialCmd(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_edit.editor.forbidden");
    }

    /// <summary>
    /// Latitude=null → location удаляется (ChangeBurialLocation(null, editor)).
    /// Save вызывается.
    /// </summary>
    [Fact]
    public async Task UpdateBurialLocation_NullCoords_ClearsLocation()
    {
        var deceased = MakeDeceasedWithLocation();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UpdateBurialLocationByEditorUseCase(
            deceasedRepo.Object, MockCurrentUser().Object, MockPolicy(allow: true).Object,
            TestExecutor.With<UpdateBurialLocationByEditorCommand, UpdateBurialLocationByEditorCommandValidator>());

        var cmd = new UpdateBurialLocationByEditorCommand(
            deceased.Id,
            Latitude: null, Longitude: null, AccuracyMeters: null,
            Country: null, Region: null, City: null,
            CemeteryName: null, PlotNumber: null, GraveNumber: null,
            Accuracy: LocationAccuracy.Exact);

        var result = await useCase.Execute(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceased.BurialLocation.Should().BeNull();
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Happy: координаты заданы → создаётся новый BurialLocation.
    /// </summary>
    [Fact]
    public async Task UpdateBurialLocation_Happy_SetsNewLocation()
    {
        var deceased = MakeDeceased();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo.Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new UpdateBurialLocationByEditorUseCase(
            deceasedRepo.Object, MockCurrentUser().Object, MockPolicy(allow: true).Object,
            TestExecutor.With<UpdateBurialLocationByEditorCommand, UpdateBurialLocationByEditorCommandValidator>());

        var result = await useCase.Execute(
            MakeBurialCmd(deceased.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceased.BurialLocation.Should().NotBeNull();
        deceased.BurialLocation!.Latitude.Should().BeApproximately(55.7558, 0.0001);
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────── GetDeceasedEditsUseCase ───────────────────────────

    /// <summary>
    /// Маппер не теряет данных: row → DeceasedEditItem с editor email/displayName.
    /// </summary>
    [Fact]
    public async Task GetDeceasedEdits_Happy_MapsRows()
    {
        var deceasedId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var deceased = MakeDeceased();
        var editResult = deceased.UpdateMainInfo(
            "Пётр", "Петров", null,
            deceased.LifePeriod.BirthDate, deceased.LifePeriod.DeathDate,
            null, null, editorId);
        editResult.IsSuccess.Should().BeTrue();
        var edit = deceased.Edits.Last();

        var rows = new List<DeceasedEditRow>
        {
            new(edit, "editor@example.com", "Editor Display"),
        };

        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo.Setup(x => x.GetEditsPaged(deceasedId, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((rows, 1));

        var useCase = new GetDeceasedEditsUseCase(
            deceasedRepo.Object,
            TestExecutor.With<GetDeceasedEditsQuery, GetDeceasedEditsQueryValidator>());

        var result = await useCase.Execute(
            new GetDeceasedEditsQuery(deceasedId, 1, 50), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        var item = result.Value.Items.Single();
        item.EditedByEmail.Should().Be("editor@example.com");
        item.EditedByDisplayName.Should().Be("Editor Display");
        item.Kind.Should().Be(DeceasedEditKind.MainInfo);
    }

    // ─────────────────────────── GetAllEditsUseCase ───────────────────────────

    /// <summary>
    /// Маппер передаёт все фильтры в репо + не теряет deceasedFullName и
    /// editor-инфу при маппинге.
    /// </summary>
    [Fact]
    public async Task GetAllEdits_Happy_PassesFiltersAndMapsRows()
    {
        var deceasedId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var deceased = MakeDeceased();
        deceased.UpdateMainInfo("X", "Y", null,
            deceased.LifePeriod.BirthDate, deceased.LifePeriod.DeathDate,
            null, null, editorId);
        var edit = deceased.Edits.Last();

        var rows = new List<DeceasedEditWithCardRow>
        {
            new(edit, "Иван Иванов", "editor@example.com", null),
        };

        var deceasedRepo = new Mock<IDeceasedRepository>();
        deceasedRepo.Setup(x => x.GetAllEditsPaged(
                1, 50, deceasedId, editorId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync((rows, 1));

        var useCase = new GetAllEditsUseCase(
            deceasedRepo.Object,
            TestExecutor.With<GetAllEditsQuery, GetAllEditsQueryValidator>());

        var result = await useCase.Execute(
            new GetAllEditsQuery(1, 50, deceasedId, editorId, from, to),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        var item = result.Value.Items.Single();
        item.DeceasedFullName.Should().Be("Иван Иванов");
        item.EditedByEmail.Should().Be("editor@example.com");
        // Verify передачи фильтров в репо.
        deceasedRepo.Verify(x => x.GetAllEditsPaged(
            1, 50, deceasedId, editorId, from, to, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─────────────────────────── Helpers ───────────────────────────

    private static (Mock<IUserRepository>, Mock<ICurrentUserService>, CanEditDeceasedPolicy) BuildPolicy()
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var policy = new CanEditDeceasedPolicy(currentUser.Object, userRepo.Object);
        return (userRepo, currentUser, policy);
    }

    private static Mock<ICurrentUserService> MockCurrentUser(Guid? userId = null)
    {
        var m = new Mock<ICurrentUserService>();
        m.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(userId ?? Guid.NewGuid()));
        return m;
    }

    private static Mock<ICanEditDeceasedPolicy> MockPolicy(bool allow)
    {
        var m = new Mock<ICanEditDeceasedPolicy>();
        m.Setup(x => x.CheckAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allow
                ? UnitResult.Success<Error>()
                : Errors.DeceasedEdit.NotEditor());
        return m;
    }

    private static Deceased MakeDeceased() =>
        Deceased.Create(
            "Иван", "Иванов", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null, CardAuthorId).Value;

    private static Deceased MakeDeceasedWithLocation()
    {
        var d = MakeDeceased();
        var loc = BurialLocation.Create(
            55.7558, 37.6173, "Россия", null, "Москва",
            null, null, null, LocationAccuracy.Exact, null).Value;
        d.ChangeBurialLocation(loc, CardAuthorId);
        return d;
    }

    private static UpdateMainInfoByEditorCommand MakeMainInfoCmd(Guid deceasedId) =>
        new(deceasedId, "Пётр", "Петров", null,
            BirthDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-50)),
            DeathDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ShortDescription: null, Biography: null);

    private static UpdateBurialLocationByEditorCommand MakeBurialCmd(Guid deceasedId) =>
        new(deceasedId,
            Latitude: 55.7558, Longitude: 37.6173, AccuracyMeters: null,
            Country: "Россия", Region: null, City: "Москва",
            CemeteryName: null, PlotNumber: null, GraveNumber: null,
            Accuracy: LocationAccuracy.Exact);
}