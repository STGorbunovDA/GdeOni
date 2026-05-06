using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.ChangeEmail.Model;
using GdeOni.Application.Users.Commands.ChangeEmail.UseCase;
using GdeOni.Application.Users.Commands.ChangeEmail.Validation;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты <see cref="ChangeEmailUseCase"/>: self/admin success +
/// RevokeAllForUser (D7.41), email-конфликт, outsider → forbidden.
/// </summary>
public sealed class ChangeEmailUseCaseTests
{
    /// <summary>
    /// Happy path: self меняет email → Save + RevokeAllForUser.
    /// SecurityStamp ротируется в домене (User.ChangeEmail), что
    /// инвалидирует существующие access-токены на следующем check'е.
    /// </summary>
    [Fact]
    public async Task Execute_Self_SavesAndRevokesAllRefreshTokens()
    {
        var user = User.Register("alice@example.com", "hash").Value;
        var oldStamp = user.SecurityStamp;

        var (userRepo, refreshRepo, currentUser, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepo.Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await useCase.Execute(
            new ChangeEmailCommand(user.Id, "alice2@example.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Email.Should().Be("alice2@example.com");
        user.SecurityStamp.Should().NotBe(oldStamp);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        refreshRepo.Verify(
            x => x.RevokeAllForUser(user.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Email уже занят другим юзером → EmailAlreadyExists. Save и
    /// RevokeAllForUser не вызываются.
    /// </summary>
    [Fact]
    public async Task Execute_EmailAlreadyTakenByAnother_ReturnsEmailAlreadyExists()
    {
        var user = User.Register("alice@example.com", "hash").Value;

        var (userRepo, refreshRepo, currentUser, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepo.Setup(x => x.ExistsByEmail("taken@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await useCase.Execute(
            new ChangeEmailCommand(user.Id, "taken@example.com"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.email.already.exists");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(
            x => x.RevokeAllForUser(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Outsider (не self, не admin) → UserForbidden.
    /// </summary>
    [Fact]
    public async Task Execute_Outsider_ReturnsUserForbidden()
    {
        var target = User.Register("bob@example.com", "hash").Value;

        var (userRepo, _, currentUser, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new ChangeEmailCommand(target.Id, "hacked@example.com"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.forbidden");
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<IRefreshTokenRepository> RefreshRepo,
        Mock<ICurrentUserService> CurrentUser,
        ChangeEmailUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var refreshRepo = new Mock<IRefreshTokenRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var useCase = new ChangeEmailUseCase(
            userRepo.Object,
            refreshRepo.Object,
            currentUser.Object,
            TestExecutor.With<ChangeEmailCommand, ChangeEmailCommandValidator>());
        return (userRepo, refreshRepo, currentUser, useCase);
    }
}
