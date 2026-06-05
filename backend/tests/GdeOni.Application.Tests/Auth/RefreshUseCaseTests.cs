using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Auth.Refresh.Model;
using GdeOni.Application.Auth.Refresh.UseCase;
using GdeOni.Application.Auth.Refresh.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.Auth;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Auth;

/// <summary>
/// Тесты <see cref="RefreshUseCase"/>: rotation happy path, replay-detection
/// (D7.32 — revoke всех RT при попытке переиспользовать revoked токен),
/// expired/unknown token. Юзер-репо мокаем; домен-объекты — настоящие.
/// </summary>
public sealed class RefreshUseCaseTests
{
    private static readonly JwtOptions JwtOptions = new()
    {
        Issuer = "test",
        Audience = "test",
        SecretKey = "test-secret-key-with-at-least-32-bytes!!",
        AccessTokenLifetimeMinutes = 30,
        RefreshTokenLifetimeDays = 7,
        SecurityStampCacheTtlSeconds = 30
    };

    /// <summary>
    /// Unknown token (не нашли по hash) → TokenInvalid.
    /// </summary>
    [Fact]
    public async Task Execute_UnknownToken_ReturnsTokenInvalid()
    {
        var (refreshRepo, _, _, factory, _, useCase) = BuildHarness();

        factory.Setup(x => x.Hash("plain")).Returns("hash");
        refreshRepo
            .Setup(x => x.GetByHash("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await useCase.Execute(
            new RefreshCommand("plain"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("refresh_token.invalid");
    }

    /// <summary>
    /// Replay revoked-токена → ReplayDetected + RevokeAllForUser
    /// (D7.32: компрометация семьи, ревокаем все активные RT).
    /// </summary>
    [Fact]
    public async Task Execute_RevokedTokenReplay_RevokesAllAndReturnsReplayDetected()
    {
        var (refreshRepo, _, _, factory, _, useCase) = BuildHarness();

        var userId = Guid.NewGuid();
        var token = RefreshToken.Issue(
            userId, "hash", DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow, "127.0.0.1").Value;
        token.Revoke(DateTime.UtcNow.AddMinutes(-1));

        factory.Setup(x => x.Hash("plain")).Returns("hash");
        refreshRepo
            .Setup(x => x.GetByHash("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var result = await useCase.Execute(
            new RefreshCommand("plain"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("refresh_token.replay_detected");
        refreshRepo.Verify(
            x => x.RevokeAllForUser(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Expired token (не revoked, но ExpiresAtUtc < now) → TokenExpired.
    /// </summary>
    [Fact]
    public async Task Execute_ExpiredToken_ReturnsTokenExpired()
    {
        var (refreshRepo, _, _, factory, _, useCase) = BuildHarness();

        var token = RefreshToken.Issue(
            Guid.NewGuid(), "hash",
            DateTime.UtcNow.AddSeconds(1), // только что выпущенный...
            DateTime.UtcNow.AddSeconds(-1)).Value;
        // ...а потом подменим ExpiresAtUtc на прошлое (Issue не позволит создать).
        typeof(RefreshToken).GetProperty(nameof(RefreshToken.ExpiresAtUtc))!
            .SetValue(token, DateTime.UtcNow.AddDays(-1));

        factory.Setup(x => x.Hash("plain")).Returns("hash");
        refreshRepo
            .Setup(x => x.GetByHash("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var result = await useCase.Execute(
            new RefreshCommand("plain"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("refresh_token.expired");
    }

    /// <summary>
    /// F17.10. Заблокированный юзер пытается обновить токен → AccountBlocked
    /// + все RT юзера ревокаются (чтобы цепочка оборвалась здесь и сейчас,
    /// а не дожила до конца жизни refresh-токена).
    /// </summary>
    [Fact]
    public async Task Execute_BlockedUser_ReturnsAccountBlockedAndRevokesAll()
    {
        var (refreshRepo, userRepo, _, factory, _, useCase) = BuildHarness();

        var user = User.Register("blocked@example.com", "$hash").Value;
        user.Block(Guid.NewGuid(), "spam", DateTime.UtcNow);

        var token = RefreshToken.Issue(
            user.Id, "hash", DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow).Value;

        factory.Setup(x => x.Hash("plain")).Returns("hash");
        refreshRepo
            .Setup(x => x.GetByHash("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        userRepo
            .Setup(x => x.GetByIdReadOnly(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(
            new RefreshCommand("plain"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.account.blocked");
        // Все RT юзера ревокнуты — повторный /refresh точно так же получит
        // AccountBlocked (replay-проверки не сработают: RevokeAllForUser
        // помечает existingToken тоже).
        refreshRepo.Verify(
            x => x.RevokeAllForUser(user.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        // Новый токен НЕ выпущен — Add не вызывался.
        refreshRepo.Verify(
            x => x.Add(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Happy path: ротация → старый токен revoked со ссылкой на новый,
    /// новый Add'ится, Save вызывается, возвращается новая пара.
    /// </summary>
    [Fact]
    public async Task Execute_HappyPath_RotatesPair()
    {
        var (refreshRepo, userRepo, jwt, factory, _, useCase) = BuildHarness();

        var user = User.Register("john@example.com", "$bcrypt-hash").Value;
        var token = RefreshToken.Issue(
            user.Id, "hash", DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow).Value;

        factory.Setup(x => x.Hash("plain")).Returns("hash");
        factory.Setup(x => x.Generate()).Returns("new-plain");
        factory.Setup(x => x.Hash("new-plain")).Returns("new-hash");
        refreshRepo
            .Setup(x => x.GetByHash("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        userRepo
            .Setup(x => x.GetByIdReadOnly(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        jwt.Setup(x => x.GenerateAccessToken(user))
            .Returns(new AccessToken("jwt", DateTime.UtcNow.AddMinutes(30)));

        var result = await useCase.Execute(
            new RefreshCommand("plain"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("new-plain");
        result.Value.AccessToken.Should().Be("jwt");
        token.IsRevoked.Should().BeTrue();
        token.ReplacedByTokenHash.Should().Be("new-hash");
        refreshRepo.Verify(
            x => x.Add(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
        refreshRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (
        Mock<IRefreshTokenRepository> RefreshRepo,
        Mock<IUserRepository> UserRepo,
        Mock<IJwtProvider> Jwt,
        Mock<IRefreshTokenFactory> Factory,
        Mock<ICurrentUserService> CurrentUser,
        RefreshUseCase UseCase) BuildHarness()
    {
        var refreshRepo = new Mock<IRefreshTokenRepository>();
        var userRepo = new Mock<IUserRepository>();
        var jwt = new Mock<IJwtProvider>();
        var factory = new Mock<IRefreshTokenFactory>();
        var currentUser = new Mock<ICurrentUserService>();

        var useCase = new RefreshUseCase(
            refreshRepo.Object,
            userRepo.Object,
            jwt.Object,
            factory.Object,
            currentUser.Object,
            Options.Create(JwtOptions),
            TestExecutor.With<RefreshCommand, RefreshCommandValidator>(),
            NullLogger<RefreshUseCase>.Instance);

        return (refreshRepo, userRepo, jwt, factory, currentUser, useCase);
    }
}
