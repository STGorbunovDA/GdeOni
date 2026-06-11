using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.Unblock.Model;
using GdeOni.Application.Users.Commands.Unblock.UseCase;
using GdeOni.Application.Users.Commands.Unblock.Validation;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты <see cref="UnblockUserUseCase"/> — F17.10. Иерархия зеркальна
/// BlockUserUseCase: Admin не может разблокировать другого Admin.
/// </summary>
public sealed class UnblockUserUseCaseTests
{
    [Fact]
    public async Task Execute_NonAdmin_ReturnsUserForbidden()
    {
        var (userRepo, currentUser, useCase) = BuildHarness(isAdmin: false, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));

        var result = await useCase.Execute(
            new UnblockUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.forbidden");
        userRepo.Verify(
            x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_AdminUnblockingPeerAdmin_ReturnsBlockPeerAdminForbidden()
    {
        var targetAdmin = User.Register("admin@example.com", "$hash").Value;
        targetAdmin.ChangeRole(UserRole.Admin);

        var (userRepo, currentUser, useCase) = BuildHarness(isAdmin: true, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        userRepo
            .Setup(x => x.GetById(targetAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAdmin);

        var result = await useCase.Execute(
            new UnblockUserCommand(targetAdmin.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.block.peer_admin.forbidden");
    }

    [Fact]
    public async Task Execute_AdminUnblockingRegular_ClearsBlockedFields()
    {
        var targetUser = User.Register("user@example.com", "$hash").Value;
        targetUser.Block(Guid.NewGuid(), "spam", DateTime.UtcNow);
        targetUser.IsBlocked.Should().BeTrue();

        var (userRepo, currentUser, useCase) = BuildHarness(isAdmin: true, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        userRepo
            .Setup(x => x.GetById(targetUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var result = await useCase.Execute(
            new UnblockUserCommand(targetUser.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        targetUser.IsBlocked.Should().BeFalse();
        targetUser.BlockedReason.Should().BeNull();
        targetUser.BlockedAtUtc.Should().BeNull();
        targetUser.BlockedByUserId.Should().BeNull();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_UnblockingNotBlocked_IsIdempotent()
    {
        var targetUser = User.Register("user@example.com", "$hash").Value;

        var (userRepo, currentUser, useCase) = BuildHarness(isAdmin: true, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        userRepo
            .Setup(x => x.GetById(targetUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var result = await useCase.Execute(
            new UnblockUserCommand(targetUser.Id), CancellationToken.None);

        // Идемпотентен: возвращает Success, юзер не менялся.
        result.IsSuccess.Should().BeTrue();
        targetUser.IsBlocked.Should().BeFalse();
        // Save всё равно вызывается — это не критично (no-op SaveChanges
        // EF Core'а быстрее проверки за нас), use case не пытается оптимизировать.
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (
        Mock<IUserRepository>,
        Mock<ICurrentUserService>,
        UnblockUserUseCase) BuildHarness(bool isAdmin, bool isSuperAdmin)
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser.Setup(x => x.IsAdmin()).Returns(isAdmin);
        currentUser
            .Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin)))
            .Returns(isSuperAdmin);

        var useCase = new UnblockUserUseCase(
            userRepo.Object,
            currentUser.Object,
            TestExecutor.With<UnblockUserCommand, UnblockUserCommandValidator>());

        return (userRepo, currentUser, useCase);
    }
}
