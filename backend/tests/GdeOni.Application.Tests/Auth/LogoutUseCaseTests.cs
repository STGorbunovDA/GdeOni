using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Auth.Logout.Model;
using GdeOni.Application.Auth.Logout.UseCase;
using GdeOni.Application.Auth.Logout.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.Auth;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Auth;

/// <summary>
/// Тесты <see cref="LogoutUseCase"/> — D7.40 идемпотентность.
/// Logout должен возвращать одинаковый Success для трёх "плохих"
/// случаев: токен не существует / уже revoked / чужой. Иначе по
/// latency / response-кодам можно перебрать чужие токены.
/// </summary>
public sealed class LogoutUseCaseTests
{
    private static readonly Guid CurrentUserId = Guid.NewGuid();

    /// <summary>
    /// Несуществующий токен → Success (Save не вызывался). Атакующий
    /// не отличает "у меня нет токена" от "токен валиден и отозван".
    /// </summary>
    [Fact]
    public async Task Execute_UnknownToken_ReturnsSuccessWithoutSave()
    {
        var (refreshRepo, factory, useCase) = BuildHarness();

        factory.Setup(x => x.Hash("plain")).Returns("hash");
        refreshRepo
            .Setup(x => x.GetByHash("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await useCase.Execute(
            new LogoutCommand("plain"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        refreshRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Чужой токен (UserId != currentUserId) → Success без Save.
    /// Защита от targeted-revoke: нельзя зайти под одним пользователем
    /// и логаутить токены другого (зная их).
    /// </summary>
    [Fact]
    public async Task Execute_TokenBelongsToAnotherUser_ReturnsSuccessWithoutSave()
    {
        var (refreshRepo, factory, useCase) = BuildHarness();

        var foreignToken = RefreshToken.Issue(
            userId: Guid.NewGuid(), // ← не CurrentUserId.
            tokenHash: "hash",
            expiresAtUtc: DateTime.UtcNow.AddDays(1),
            nowUtc: DateTime.UtcNow,
            createdFromIp: "127.0.0.1").Value;

        factory.Setup(x => x.Hash("plain")).Returns("hash");
        refreshRepo
            .Setup(x => x.GetByHash("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignToken);

        var result = await useCase.Execute(
            new LogoutCommand("plain"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        refreshRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Уже revoked → Success без Save (повторный logout того же
    /// токена не должен ругаться 409).
    /// </summary>
    [Fact]
    public async Task Execute_AlreadyRevokedToken_ReturnsSuccessWithoutSave()
    {
        var (refreshRepo, factory, useCase) = BuildHarness();

        var token = RefreshToken.Issue(
            userId: CurrentUserId,
            tokenHash: "hash",
            expiresAtUtc: DateTime.UtcNow.AddDays(1),
            nowUtc: DateTime.UtcNow,
            createdFromIp: "127.0.0.1").Value;
        token.Revoke(DateTime.UtcNow); // pre-revoked

        factory.Setup(x => x.Hash("plain")).Returns("hash");
        refreshRepo
            .Setup(x => x.GetByHash("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var result = await useCase.Execute(
            new LogoutCommand("plain"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        refreshRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Happy path: свой активный токен — отзывается и Save вызывается.
    /// </summary>
    [Fact]
    public async Task Execute_OwnActiveToken_RevokesAndSaves()
    {
        var (refreshRepo, factory, useCase) = BuildHarness();

        var token = RefreshToken.Issue(
            userId: CurrentUserId,
            tokenHash: "hash",
            expiresAtUtc: DateTime.UtcNow.AddDays(1),
            nowUtc: DateTime.UtcNow,
            createdFromIp: "127.0.0.1").Value;

        factory.Setup(x => x.Hash("plain")).Returns("hash");
        refreshRepo
            .Setup(x => x.GetByHash("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var result = await useCase.Execute(
            new LogoutCommand("plain"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        refreshRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (
        Mock<IRefreshTokenRepository>,
        Mock<IRefreshTokenFactory>,
        LogoutUseCase) BuildHarness()
    {
        var refreshRepo = new Mock<IRefreshTokenRepository>();
        var factory = new Mock<IRefreshTokenFactory>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(CurrentUserId));

        var useCase = new LogoutUseCase(
            refreshRepo.Object,
            factory.Object,
            currentUser.Object,
            TestExecutor.With<LogoutCommand, LogoutCommandValidator>());

        return (refreshRepo, factory, useCase);
    }
}
