using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Routing;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.Model;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.UseCase;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Routing;

/// <summary>
/// Тесты <see cref="GetRouteToGraveUseCase"/> — главный use case
/// сценария "Построить маршрут к могиле".
///
/// Покрываем три ключевых ветки:
/// 1) пользователь не трекает этого deceased → 403 (NotTracked);
/// 2) у deceased не задано BurialLocation → 409 (BurialLocationNotSet);
/// 3) happy path — собираются deep-link'и через всех IRouteLinkProvider.
///
/// Use case ходит в IUserRepository.IsActivelyTracking (EXISTS),
/// потом в IDeceasedRepository.GetByIdReadOnly (read-only),
/// потом итерирует IEnumerable&lt;IRouteLinkProvider&gt;.
/// </summary>
public sealed class GetRouteToGraveUseCaseTests
{
    private static readonly Guid SampleUserId = Guid.NewGuid();
    private static readonly Guid SampleDeceasedId = Guid.NewGuid();
    private const double FromLat = 55.7558;
    private const double FromLon = 37.6173;

    /// <summary>
    /// Если IsActivelyTracking вернул false — пользователь не подписан
    /// на этого deceased. Маршрут к чужой могиле через эндпоинт
    /// /me/tracked-deceased/{id}/route не должен раскрываться: иначе
    /// можно перебором deceasedId узнать координаты любой могилы
    /// в системе. Use case возвращает Forbidden / `tracking.not_tracked`,
    /// и до GetByIdReadOnly даже не доходит.
    /// </summary>
    [Fact]
    public async Task Execute_UserDoesNotTrackDeceased_ReturnsNotTracked()
    {
        // Arrange: моки.
        var userRepo = new Mock<IUserRepository>();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        // currentUserService возвращает успех — пользователь авторизован.
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(SampleUserId));

        // ключевой setup: трекинга НЕТ.
        userRepo
            .Setup(x => x.IsActivelyTracking(SampleUserId, SampleDeceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new GetRouteToGraveUseCase(
            userRepo.Object,
            deceasedRepo.Object,
            routeLinkProviders: Array.Empty<IRouteLinkProvider>(),
            currentUser.Object,
            TestExecutor.With<GetRouteToGraveQuery, GetRouteToGraveQueryValidator>());

        // Act
        var result = await useCase.Execute(
            new GetRouteToGraveQuery(SampleDeceasedId, FromLat, FromLon, RoutingMode.Auto),
            CancellationToken.None);

        // Assert: 403 / `tracking.not_tracked`. И DeceasedRepo.GetByIdReadOnly
        // НЕ вызывался — это важная гарантия: чужие координаты не
        // материализуются даже в памяти процесса.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.not_tracked");
        deceasedRepo.Verify(
            x => x.GetByIdReadOnly(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Пользователь трекает deceased, но у карточки не заданы
    /// координаты (карточка создавалась без burial — общий /create,
    /// а не /at-grave). Маршрут построить нельзя физически — use case
    /// возвращает 409 / `deceased.burial_location.not_set`. Клиент
    /// в UI покажет "сначала укажите место захоронения" вместо
    /// "что-то пошло не так".
    /// </summary>
    [Fact]
    public async Task Execute_DeceasedHasNoBurialLocation_ReturnsBurialLocationNotSet()
    {
        // Arrange: трекинг есть, но BurialLocation = null.
        var userRepo = new Mock<IUserRepository>();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(SampleUserId));

        userRepo
            .Setup(x => x.IsActivelyTracking(SampleUserId, SampleDeceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Карточка без BurialLocation — для тестов хватит создания
        // через настоящий Domain.Create (а не Mock), потому что
        // конструкторы Deceased приватные.
        var deceased = Deceased.Create(
            firstName: "Иван",
            lastName: "Иванов",
            middleName: null,
            birthDate: null,
            deathDate: new DateOnly(2010, 1, 1),
            burialLocation: null,
            createdByUserId: Guid.NewGuid()).Value;

        deceasedRepo
            .Setup(x => x.GetByIdReadOnly(SampleDeceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new GetRouteToGraveUseCase(
            userRepo.Object,
            deceasedRepo.Object,
            routeLinkProviders: Array.Empty<IRouteLinkProvider>(),
            currentUser.Object,
            TestExecutor.With<GetRouteToGraveQuery, GetRouteToGraveQueryValidator>());

        // Act
        var result = await useCase.Execute(
            new GetRouteToGraveQuery(SampleDeceasedId, FromLat, FromLon, RoutingMode.Auto),
            CancellationToken.None);

        // Assert: 409 / `deceased.burial_location.not_set`.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.burial_location.not_set");
    }

    /// <summary>
    /// Happy path: пользователь трекает, у deceased есть burial.
    /// Use case возвращает координаты + по одной ссылке на каждый
    /// зарегистрированный IRouteLinkProvider. ProviderKey попадает
    /// в Result.Links как идентификатор; URL — то, что вернул
    /// BuildLink провайдера. Нам важно убедиться, что:
    /// - все провайдеры дёрнуты ровно по разу;
    /// - каждый получил координаты пользователя (FromLat/FromLon),
    ///   могилы (GraveLat/GraveLon) и Mode из запроса;
    /// - результат содержит links с правильными ProviderKey'ами.
    /// </summary>
    [Fact]
    public async Task Execute_HappyPath_ReturnsLinksFromAllProviders()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(SampleUserId));

        userRepo
            .Setup(x => x.IsActivelyTracking(SampleUserId, SampleDeceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Создаём deceased с координатами Москвы.
        var graveLocation = BurialLocation.Create(55.7558, 37.6173).Value;
        var deceased = Deceased.Create(
            firstName: "Иван",
            lastName: "Иванов",
            middleName: null,
            birthDate: null,
            deathDate: new DateOnly(2010, 1, 1),
            burialLocation: graveLocation,
            createdByUserId: Guid.NewGuid()).Value;

        deceasedRepo
            .Setup(x => x.GetByIdReadOnly(SampleDeceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        // Два mock-провайдера — yandex и google. BuildLink проверяет,
        // что use case передал именно из/в координаты и Mode.
        var yandex = new Mock<IRouteLinkProvider>();
        yandex.Setup(x => x.ProviderKey).Returns("yandex");
        yandex
            .Setup(x => x.BuildLink(FromLat, FromLon, graveLocation.Latitude, graveLocation.Longitude, RoutingMode.Auto))
            .Returns("https://yandex.ru/maps/...");

        var google = new Mock<IRouteLinkProvider>();
        google.Setup(x => x.ProviderKey).Returns("google");
        google
            .Setup(x => x.BuildLink(FromLat, FromLon, graveLocation.Latitude, graveLocation.Longitude, RoutingMode.Auto))
            .Returns("https://google.com/maps/...");

        var useCase = new GetRouteToGraveUseCase(
            userRepo.Object,
            deceasedRepo.Object,
            routeLinkProviders: new[] { yandex.Object, google.Object },
            currentUser.Object,
            TestExecutor.With<GetRouteToGraveQuery, GetRouteToGraveQueryValidator>());

        // Act
        var result = await useCase.Execute(
            new GetRouteToGraveQuery(SampleDeceasedId, FromLat, FromLon, RoutingMode.Auto),
            CancellationToken.None);

        // Assert: use case возвращает DeceasedId из доменного аггрегата
        // (не из query — ровно тот, что лежит в БД). В тесте репозиторий
        // отдал deceased с собственным сгенерированным Id, его и проверяем.
        result.IsSuccess.Should().BeTrue();
        result.Value.DeceasedId.Should().Be(deceased.Id);
        result.Value.GraveLat.Should().Be(graveLocation.Latitude);
        result.Value.GraveLon.Should().Be(graveLocation.Longitude);
        result.Value.FromLat.Should().Be(FromLat);
        result.Value.FromLon.Should().Be(FromLon);
        result.Value.Mode.Should().Be(RoutingMode.Auto);

        // Проверяем, что в результате — 2 link'а с ожидаемыми
        // ProviderKey-ями. Порядок соответствует порядку IEnumerable
        // (yandex первым, потому что мы передали его первым).
        result.Value.Links.Should().HaveCount(2);
        result.Value.Links.Select(l => l.Provider).Should().BeEquivalentTo(new[] { "yandex", "google" });
        result.Value.Links.Select(l => l.Url).Should().AllSatisfy(url =>
            url.Should().StartWith("https://"));
    }

    /// <summary>
    /// Если currentUserService.GetCurrentUserId вернул Failure
    /// (Unauthorized) — use case сразу отдаёт эту ошибку, не ходит ни
    /// в репозитории, ни в провайдеров. Это страховка на случай,
    /// если кто-то снимет [Authorize] с эндпоинта: domain layer
    /// сам перехватит отсутствие auth.
    /// </summary>
    [Fact]
    public async Task Execute_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        // CurrentUserService возвращает Unauthorized.
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Failure<Guid, Error>(Errors.General.Unauthorized()));

        var useCase = new GetRouteToGraveUseCase(
            userRepo.Object,
            deceasedRepo.Object,
            routeLinkProviders: Array.Empty<IRouteLinkProvider>(),
            currentUser.Object,
            TestExecutor.With<GetRouteToGraveQuery, GetRouteToGraveQueryValidator>());

        // Act
        var result = await useCase.Execute(
            new GetRouteToGraveQuery(SampleDeceasedId, FromLat, FromLon, RoutingMode.Auto),
            CancellationToken.None);

        // Assert: ошибка авторизации — и ни одного похода в БД.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.unauthorized");
        userRepo.Verify(
            x => x.IsActivelyTracking(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
