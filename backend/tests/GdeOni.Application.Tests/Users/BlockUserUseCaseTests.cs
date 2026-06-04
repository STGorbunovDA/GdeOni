using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.Block.Model;
using GdeOni.Application.Users.Commands.Block.UseCase;
using GdeOni.Application.Users.Commands.Block.Validation;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты <see cref="BlockUserUseCase"/> — F17.10. Покрывают права доступа
/// и иерархию (Admin не может блокировать другого Admin), self-block guard,
/// SuperAdmin guard и happy path с ротацией SecurityStamp + Invalidate.
/// </summary>
public sealed class BlockUserUseCaseTests
{
    [Fact]
    public async Task Execute_NonAdmin_ReturnsUserForbidden()
    {
        var (userRepo, currentUser, _, useCase) = BuildHarness(isAdmin: false, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));

        var result = await useCase.Execute(
            new BlockUserCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.forbidden");
        userRepo.Verify(
            x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_BlockingSelf_ReturnsBlockSelfForbidden()
    {
        var currentUserId = Guid.NewGuid();
        var (_, currentUser, _, useCase) = BuildHarness(isAdmin: true, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));

        var result = await useCase.Execute(
            new BlockUserCommand(currentUserId, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.block.self.forbidden");
    }

    [Fact]
    public async Task Execute_TargetIsSuperAdmin_ReturnsBlockSuperAdminForbidden()
    {
        var currentUserId = Guid.NewGuid();
        var targetSuperAdmin = User.RegisterSuperAdmin("super@example.com", "$hash").Value;

        var (userRepo, currentUser, _, useCase) = BuildHarness(isAdmin: true, isSuperAdmin: true);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));
        userRepo
            .Setup(x => x.GetById(targetSuperAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetSuperAdmin);

        var result = await useCase.Execute(
            new BlockUserCommand(targetSuperAdmin.Id, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.block.super_admin.forbidden");
    }

    [Fact]
    public async Task Execute_AdminBlockingPeerAdmin_ReturnsBlockPeerAdminForbidden()
    {
        var currentUserId = Guid.NewGuid();
        var targetAdmin = User.Register("admin@example.com", "$hash").Value;
        targetAdmin.ChangeRole(UserRole.Admin);

        var (userRepo, currentUser, _, useCase) = BuildHarness(isAdmin: true, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));
        userRepo
            .Setup(x => x.GetById(targetAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAdmin);

        var result = await useCase.Execute(
            new BlockUserCommand(targetAdmin.Id, "abuse"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.block.peer_admin.forbidden");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_AdminBlockingRegularUser_SucceedsAndInvalidatesStamp()
    {
        var currentUserId = Guid.NewGuid();
        var targetUser = User.Register("user@example.com", "$hash").Value;
        var initialStamp = targetUser.SecurityStamp;

        var (userRepo, currentUser, invalidator, useCase) = BuildHarness(isAdmin: true, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));
        userRepo
            .Setup(x => x.GetById(targetUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var result = await useCase.Execute(
            new BlockUserCommand(targetUser.Id, "  spam  "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(targetUser.Id);
        targetUser.IsBlocked.Should().BeTrue();
        // Trim прошёл — reason без пробелов по краям.
        targetUser.BlockedReason.Should().Be("spam");
        targetUser.BlockedByUserId.Should().Be(currentUserId);
        // SecurityStamp ротирован — без этого access-токены живут до конца TTL.
        targetUser.SecurityStamp.Should().NotBe(initialStamp);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        invalidator.Verify(x => x.Invalidate(targetUser.Id), Times.Once);
    }

    [Fact]
    public async Task Execute_SuperAdminBlockingAdmin_Succeeds()
    {
        var currentUserId = Guid.NewGuid();
        var targetAdmin = User.Register("admin@example.com", "$hash").Value;
        targetAdmin.ChangeRole(UserRole.Admin);

        var (userRepo, currentUser, _, useCase) = BuildHarness(isAdmin: true, isSuperAdmin: true);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));
        userRepo
            .Setup(x => x.GetById(targetAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAdmin);

        var result = await useCase.Execute(
            new BlockUserCommand(targetAdmin.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        targetAdmin.IsBlocked.Should().BeTrue();
        targetAdmin.BlockedReason.Should().BeNull();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (
        Mock<IUserRepository>,
        Mock<ICurrentUserService>,
        Mock<ISecurityStampInvalidator>,
        BlockUserUseCase) BuildHarness(bool isAdmin, bool isSuperAdmin)
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var invalidator = new Mock<ISecurityStampInvalidator>();

        currentUser.Setup(x => x.IsAdmin()).Returns(isAdmin);
        currentUser
            .Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin)))
            .Returns(isSuperAdmin);

        var useCase = new BlockUserUseCase(
            userRepo.Object,
            currentUser.Object,
            invalidator.Object,
            TestExecutor.With<BlockUserCommand, BlockUserCommandValidator>());

        return (userRepo, currentUser, invalidator, useCase);
    }
}
