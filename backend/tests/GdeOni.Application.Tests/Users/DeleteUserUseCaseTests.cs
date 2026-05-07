using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.Delete.Model;
using GdeOni.Application.Users.Commands.Delete.UseCase;
using GdeOni.Application.Users.Commands.Delete.Validation;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты <see cref="DeleteUserUseCase"/> — критичная логика прав
/// доступа к удалению пользователей. Покрывает:
/// — non-admin вообще не может удалять (UserForbidden);
/// — нельзя удалить себя (DeleteSelfForbidden);
/// — нельзя удалить SuperAdmin'а (DeleteSuperAdminForbidden);
/// — Admin не может удалить другого Admin (D7.70).
/// </summary>
public sealed class DeleteUserUseCaseTests
{
    /// <summary>
    /// Не-админ пытается удалить кого угодно → UserForbidden.
    /// До GetById даже не доходит — исключаем загрузку чужого user
    /// в память.
    /// </summary>
    [Fact]
    public async Task Execute_NonAdmin_ReturnsUserForbidden()
    {
        var (userRepo, currentUser, _, useCase) = BuildHarness(isAdmin: false, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));

        var result = await useCase.Execute(
            new DeleteUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.forbidden");
        userRepo.Verify(
            x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Admin пытается удалить себя → DeleteSelfForbidden.
    /// Защита от случайного "удалил админ-аккаунт через UI".
    /// </summary>
    [Fact]
    public async Task Execute_DeletingSelf_ReturnsDeleteSelfForbidden()
    {
        var currentUserId = Guid.NewGuid();
        var (userRepo, currentUser, _, useCase) = BuildHarness(isAdmin: true, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));

        var result = await useCase.Execute(
            new DeleteUserCommand(currentUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.delete.self.forbidden");
    }

    /// <summary>
    /// Удалить SuperAdmin → DeleteSuperAdminForbidden.
    /// SuperAdmin создаётся только seeder'ом и должен оставаться
    /// в системе всегда (хотя бы один).
    /// </summary>
    [Fact]
    public async Task Execute_TargetIsSuperAdmin_ReturnsDeleteSuperAdminForbidden()
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
            new DeleteUserCommand(targetSuperAdmin.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.delete.super_admin.forbidden");
    }

    /// <summary>
    /// D7.70: Admin не может удалить другого Admin. Только SuperAdmin
    /// может снять Admin'а. Без этой защиты возможны admin-vs-admin
    /// войны через тихие удаления.
    /// </summary>
    [Fact]
    public async Task Execute_AdminDeletingPeerAdmin_ReturnsDeletePeerAdminForbidden()
    {
        var currentUserId = Guid.NewGuid();
        var targetAdmin = User.Register("admin@example.com", "$hash").Value;
        targetAdmin.ChangeRole(UserRole.Admin);

        // currentUser — Admin, но НЕ SuperAdmin.
        var (userRepo, currentUser, _, useCase) = BuildHarness(isAdmin: true, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));
        userRepo
            .Setup(x => x.GetById(targetAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAdmin);

        var result = await useCase.Execute(
            new DeleteUserCommand(targetAdmin.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.delete.peer_admin.forbidden");
        // Save НЕ вызывался — Admin сохранён.
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Happy path: Admin удаляет обычного RegularUser → success.
    /// Repository.Delete + Save вызваны.
    /// </summary>
    [Fact]
    public async Task Execute_AdminDeletingRegularUser_Succeeds()
    {
        var currentUserId = Guid.NewGuid();
        var targetUser = User.Register("user@example.com", "$hash").Value;

        var (userRepo, currentUser, invalidator, useCase) = BuildHarness(isAdmin: true, isSuperAdmin: false);
        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(currentUserId));
        userRepo
            .Setup(x => x.GetById(targetUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var result = await useCase.Execute(
            new DeleteUserCommand(targetUser.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(targetUser.Id);
        userRepo.Verify(x => x.Delete(targetUser), Times.Once);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        // D11.10.1: после удаления кеш SecurityStamp должен быть сброшен,
        // иначе access-токены удалённого user'а проживут TTL.
        invalidator.Verify(x => x.Invalidate(targetUser.Id), Times.Once);
    }

    /// <summary>
    /// Helper: собирает моки + use case за один вызов. Возвращает
    /// userRepo и currentUser для дополнительной настройки в тесте.
    /// </summary>
    private static (
        Mock<IUserRepository>,
        Mock<ICurrentUserService>,
        Mock<ISecurityStampInvalidator>,
        DeleteUserUseCase) BuildHarness(bool isAdmin, bool isSuperAdmin)
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var invalidator = new Mock<ISecurityStampInvalidator>();

        currentUser.Setup(x => x.IsAdmin()).Returns(isAdmin);
        currentUser
            .Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin)))
            .Returns(isSuperAdmin);

        var useCase = new DeleteUserUseCase(
            userRepo.Object,
            currentUser.Object,
            invalidator.Object,
            TestExecutor.With<DeleteUserCommand, DeleteUserCommandValidator>());

        return (userRepo, currentUser, invalidator, useCase);
    }
}
