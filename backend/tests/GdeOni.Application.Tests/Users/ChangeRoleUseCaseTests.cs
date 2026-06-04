using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.ChangeRole.Model;
using GdeOni.Application.Users.Commands.ChangeRole.UseCase;
using GdeOni.Application.Users.Commands.ChangeRole.Validation;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты <see cref="ChangeRoleUseCase"/>: только admin может, peer-admin
/// нельзя без SuperAdmin (D7.70), SuperAdmin неприкасаем,
/// после смены роли — RevokeAllForUser (security-event).
/// </summary>
public sealed class ChangeRoleUseCaseTests
{
    /// <summary>
    /// Не-admin пытается → UserForbidden. Save и Revoke не вызываются.
    /// </summary>
    [Fact]
    public async Task Execute_NotAdmin_ReturnsUserForbidden()
    {
        var target = User.Register("bob@example.com", "hash").Value;

        var (userRepo, refreshRepo, currentUser, invalidator, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        var result = await useCase.Execute(
            new ChangeRoleCommand(target.Id, UserRole.Admin),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.forbidden");
    }

    /// <summary>
    /// Admin → Admin без SuperAdmin → ChangePeerAdminRoleForbidden.
    /// </summary>
    [Fact]
    public async Task Execute_AdminEditsAnotherAdmin_ReturnsPeerAdminForbidden()
    {
        var target = User.Register("peer@example.com", "hash").Value;
        target.ChangeRole(UserRole.Admin);

        var (userRepo, _, currentUser, _, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        currentUser.Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin))).Returns(false);
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new ChangeRoleCommand(target.Id, UserRole.RegularUser),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.role.change.peer_admin.forbidden");
    }

    /// <summary>
    /// SuperAdmin неприкасаем для обычного admin →
    /// ChangeSuperAdminRoleForbidden.
    /// </summary>
    [Fact]
    public async Task Execute_AdminEditsSuperAdmin_ReturnsSuperAdminForbidden()
    {
        var target = User.RegisterSuperAdmin("super@example.com", "hash").Value;

        var (userRepo, _, currentUser, _, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        currentUser.Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin))).Returns(false);
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new ChangeRoleCommand(target.Id, UserRole.RegularUser),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.role.change.super_admin.forbidden");
    }

    /// <summary>
    /// Admin меняет RegularUser → Admin → success + RevokeAllForUser
    /// (security-event, новый Role в access-token-claim'ах).
    /// </summary>
    [Fact]
    public async Task Execute_AdminPromotesRegularUserToManager_SavesAndRevokesTokens()
    {
        var target = User.Register("bob@example.com", "hash").Value;
        var oldStamp = target.SecurityStamp;

        var (userRepo, refreshRepo, currentUser, invalidator, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        currentUser.Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin))).Returns(false);
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        // Admin может назначить только Manager (или RegularUser).
        var result = await useCase.Execute(
            new ChangeRoleCommand(target.Id, UserRole.Manager),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.Role.Should().Be(UserRole.Manager);
        target.SecurityStamp.Should().NotBe(oldStamp);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        refreshRepo.Verify(
            x => x.RevokeAllForUser(target.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        invalidator.Verify(x => x.Invalidate(target.Id), Times.Once);
    }

    /// <summary>
    /// Admin не может назначить роль Admin или SuperAdmin — только SuperAdmin.
    /// </summary>
    [Fact]
    public async Task Execute_AdminAssignsAdminRole_Forbidden()
    {
        var target = User.Register("bob@example.com", "hash").Value;

        var (userRepo, _, currentUser, _, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        currentUser.Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin))).Returns(false);
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new ChangeRoleCommand(target.Id, UserRole.Admin),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.role.assign.admin.forbidden");
        target.Role.Should().Be(UserRole.RegularUser);
    }

    /// <summary>
    /// D11.8.2: та же роль — domain делает no-op, use case не должен
    /// дёргать Save / RevokeAllForUser / Invalidate.
    /// </summary>
    [Fact]
    public async Task Execute_SameRole_NoOp()
    {
        var target = User.Register("bob@example.com", "hash").Value;
        target.ChangeRole(UserRole.Admin);
        var oldStamp = target.SecurityStamp;

        var (userRepo, refreshRepo, currentUser, invalidator, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        currentUser.Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin))).Returns(true);
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new ChangeRoleCommand(target.Id, UserRole.Admin),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.SecurityStamp.Should().Be(oldStamp);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(
            x => x.RevokeAllForUser(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        invalidator.Verify(x => x.Invalidate(It.IsAny<Guid>()), Times.Never);
    }

    /// <summary>
    /// Попытка ChangeRole на SuperAdmin → отвергается validator'ом
    /// (RoleSuperAdminNotAllowed). Executor агрегирует validation
    /// ошибки в Error.Validation с code "validation.failed";
    /// конкретный errorCode лежит в Errors-deталях.
    /// </summary>
    [Fact]
    public async Task Execute_RoleSuperAdmin_RejectedByValidator()
    {
        var target = User.Register("bob@example.com", "hash").Value;

        var (userRepo, _, currentUser, _, useCase) = BuildHarness();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        currentUser.Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin))).Returns(true);
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new ChangeRoleCommand(target.Id, UserRole.SuperAdmin),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Details.Should().Contain(e => e.ErrorCode == "user.role.super_admin.not_allowed");
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<IRefreshTokenRepository> RefreshRepo,
        Mock<ICurrentUserService> CurrentUser,
        Mock<ISecurityStampInvalidator> Invalidator,
        ChangeRoleUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var refreshRepo = new Mock<IRefreshTokenRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var invalidator = new Mock<ISecurityStampInvalidator>();
        var useCase = new ChangeRoleUseCase(
            userRepo.Object,
            refreshRepo.Object,
            currentUser.Object,
            invalidator.Object,
            TestExecutor.With<ChangeRoleCommand, ChangeRoleCommandValidator>());
        return (userRepo, refreshRepo, currentUser, invalidator, useCase);
    }
}
