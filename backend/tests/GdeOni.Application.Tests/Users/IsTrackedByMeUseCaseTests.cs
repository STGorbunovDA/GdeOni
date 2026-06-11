using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Queries.IsTrackedByMe.Model;
using GdeOni.Application.Users.Queries.IsTrackedByMe.UseCase;
using GdeOni.Application.Users.Queries.IsTrackedByMe.Validation;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты <see cref="IsTrackedByMeUseCase"/> — лёгкого query
/// для UI-кнопки "Отслеживать". Use case делает один SELECT 1
/// (EXISTS) через IsActivelyTracking. Тут проверяем как контракт
/// validator'а (D8.6: Guid.Empty → 400), так и happy-path'ы.
/// </summary>
public sealed class IsTrackedByMeUseCaseTests
{
    private static readonly Guid SampleUserId = Guid.NewGuid();
    private static readonly Guid SampleDeceasedId = Guid.NewGuid();

    /// <summary>
    /// D8.6: пустой Guid в DeceasedId должен отвергаться валидатором.
    /// До D8.6 use case молча возвращал Tracked=false на Guid.Empty —
    /// это не security-issue, но рассогласовано с остальным проектом.
    /// Теперь — 400 / `deceased.id.required` ровно как у других query.
    /// </summary>
    [Fact]
    public async Task Execute_EmptyDeceasedId_ReturnsValidationError()
    {
        // Arrange: моки даже не нужны — validator упадёт раньше.
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(SampleUserId));

        var useCase = new IsTrackedByMeUseCase(
            userRepo.Object,
            currentUser.Object,
            TestExecutor.With<IsTrackedByMeQuery, IsTrackedByMeQueryValidator>());

        // Act
        var result = await useCase.Execute(
            new IsTrackedByMeQuery(Guid.Empty),
            CancellationToken.None);

        // Assert: ToValidationError упаковывает все ошибки FluentValidation
        // в один Error с кодом-конвертом "validation.failed" и
        // конкретными кодами в деталях. UI/контроллер показывает 400
        // и список deceased.id.required.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation.failed");
        result.Error.Details.Should().ContainSingle(d => d.ErrorCode == "deceased.id.required");

        // IsActivelyTracking НЕ вызывался — validator перехватил до handler'а.
        userRepo.Verify(
            x => x.IsActivelyTracking(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Happy path "трекает" — IsActivelyTracking вернул true,
    /// use case отдаёт IsTrackedByMeResponse(Tracked: true).
    /// </summary>
    [Fact]
    public async Task Execute_UserTracksDeceased_ReturnsTrackedTrue()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(SampleUserId));
        userRepo
            .Setup(x => x.IsActivelyTracking(SampleUserId, SampleDeceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new IsTrackedByMeUseCase(
            userRepo.Object,
            currentUser.Object,
            TestExecutor.With<IsTrackedByMeQuery, IsTrackedByMeQueryValidator>());

        // Act
        var result = await useCase.Execute(
            new IsTrackedByMeQuery(SampleDeceasedId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tracked.Should().BeTrue();
    }

    /// <summary>
    /// Happy path "не трекает" — IsActivelyTracking вернул false,
    /// use case отдаёт Tracked: false. UI показывает кнопку "Отслеживать".
    /// </summary>
    [Fact]
    public async Task Execute_UserDoesNotTrackDeceased_ReturnsTrackedFalse()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(SampleUserId));
        userRepo
            .Setup(x => x.IsActivelyTracking(SampleUserId, SampleDeceasedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new IsTrackedByMeUseCase(
            userRepo.Object,
            currentUser.Object,
            TestExecutor.With<IsTrackedByMeQuery, IsTrackedByMeQueryValidator>());

        // Act
        var result = await useCase.Execute(
            new IsTrackedByMeQuery(SampleDeceasedId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tracked.Should().BeFalse();
    }
}
