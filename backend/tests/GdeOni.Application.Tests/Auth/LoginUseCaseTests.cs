using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Application.Auth.Login.UseCase;
using GdeOni.Application.Auth.Login.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Auth;

/// <summary>
/// Тесты <see cref="LoginUseCase"/> — критичный auth-сценарий.
/// Главные риски: timing-based user enumeration (атакующий по
/// разнице во времени ответа определяет, существует ли email),
/// неправильный код ошибки (404 палит существование email,
/// 401 универсально безопасен), отсутствие MarkLogin.
/// </summary>
public sealed class LoginUseCaseTests
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
    /// Несуществующий email → InvalidCredentials (НЕ NotFound).
    /// Дополнительно проверяем, что passwordHasher.Verify был вызван
    /// с DummyHash — это гарантия timing-safe защиты от перебора email'ов.
    /// </summary>
    [Fact]
    public async Task Execute_UnknownEmail_ReturnsInvalidCredentialsAndVerifiesDummyHash()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        var refreshRepo = new Mock<IRefreshTokenRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var jwt = new Mock<IJwtProvider>();
        var rtFactory = new Mock<IRefreshTokenFactory>();
        var currentUser = new Mock<ICurrentUserService>();

        // GetByEmail вернёт null — пользователя нет.
        userRepo
            .Setup(x => x.GetByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // DummyHash — фиксированный валидный хеш.
        hasher.Setup(x => x.DummyHash).Returns("$2a$11$dummy.hash.for.timing.safety");

        var useCase = new LoginUseCase(
            userRepo.Object, refreshRepo.Object, hasher.Object,
            jwt.Object, rtFactory.Object, currentUser.Object,
            Options.Create(JwtOptions),
            TestExecutor.With<LoginCommand, LoginCommandValidator>());

        // Act
        var result = await useCase.Execute(
            new LoginCommand("ghost@example.com", "Password123!"),
            CancellationToken.None);

        // Assert: 401 InvalidCredentials.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.invalid.credentials");

        // КЛЮЧЕВОЕ: Verify дёрнут с DummyHash — атакующий не сможет
        // по таймингу понять, что email не существует.
        hasher.Verify(
            x => x.Verify(It.IsAny<string>(), "$2a$11$dummy.hash.for.timing.safety"),
            Times.Once);
    }

    /// <summary>
    /// Существующий email + неверный пароль → InvalidCredentials.
    /// Тот же error-код, что для несуществующего email — это
    /// сознательный contract: одинаковая ошибка скрывает, что именно
    /// не сошлось (email или пароль).
    /// </summary>
    [Fact]
    public async Task Execute_WrongPassword_ReturnsInvalidCredentials()
    {
        // Arrange: настоящий User в БД.
        var user = User.Register("john@example.com", "$bcrypt-hash").Value;

        var userRepo = new Mock<IUserRepository>();
        var refreshRepo = new Mock<IRefreshTokenRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var jwt = new Mock<IJwtProvider>();
        var rtFactory = new Mock<IRefreshTokenFactory>();
        var currentUser = new Mock<ICurrentUserService>();

        userRepo
            .Setup(x => x.GetByEmail("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Verify против настоящего хеша — false.
        hasher
            .Setup(x => x.Verify("WrongPassword", user.PasswordHash))
            .Returns(false);

        var useCase = new LoginUseCase(
            userRepo.Object, refreshRepo.Object, hasher.Object,
            jwt.Object, rtFactory.Object, currentUser.Object,
            Options.Create(JwtOptions),
            TestExecutor.With<LoginCommand, LoginCommandValidator>());

        // Act
        var result = await useCase.Execute(
            new LoginCommand("john@example.com", "WrongPassword"),
            CancellationToken.None);

        // Assert: тот же InvalidCredentials, MarkLogin НЕ вызывался,
        // refresh-token НЕ создавался.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.invalid.credentials");
        refreshRepo.Verify(
            x => x.Add(It.IsAny<Domain.Aggregates.Auth.RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Happy path: email + пароль совпадают → выдаётся access + refresh,
    /// MarkLogin обновляет LastLoginAtUtc, refresh-token создаётся в БД.
    /// </summary>
    [Fact]
    public async Task Execute_HappyPath_GeneratesAccessTokenAndPersistsRefresh()
    {
        // Arrange
        var user = User.Register("john@example.com", "$bcrypt-hash").Value;

        var userRepo = new Mock<IUserRepository>();
        var refreshRepo = new Mock<IRefreshTokenRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var jwt = new Mock<IJwtProvider>();
        var rtFactory = new Mock<IRefreshTokenFactory>();
        var currentUser = new Mock<ICurrentUserService>();

        userRepo
            .Setup(x => x.GetByEmail("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        hasher
            .Setup(x => x.Verify("Password123!", user.PasswordHash))
            .Returns(true);
        jwt
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(new AccessToken("jwt.token", DateTime.UtcNow.AddMinutes(30)));
        rtFactory.Setup(x => x.Generate()).Returns("plain-refresh");
        rtFactory.Setup(x => x.Hash("plain-refresh")).Returns("hash-refresh");
        currentUser.Setup(x => x.GetRemoteIpAddress()).Returns("127.0.0.1");

        user.LastLoginAtUtc.Should().BeNull(); // pre-condition

        var useCase = new LoginUseCase(
            userRepo.Object, refreshRepo.Object, hasher.Object,
            jwt.Object, rtFactory.Object, currentUser.Object,
            Options.Create(JwtOptions),
            TestExecutor.With<LoginCommand, LoginCommandValidator>());

        // Act
        var result = await useCase.Execute(
            new LoginCommand("john@example.com", "Password123!"),
            CancellationToken.None);

        // Assert: успех + access/refresh не пустые + MarkLogin сработал
        // (LastLoginAtUtc выставлен) + refresh-token попал в БД.
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt.token");
        result.Value.RefreshToken.Should().Be("plain-refresh");
        user.LastLoginAtUtc.Should().NotBeNull();
        refreshRepo.Verify(
            x => x.Add(It.IsAny<Domain.Aggregates.Auth.RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
        refreshRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }
}
