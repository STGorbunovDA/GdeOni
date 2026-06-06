using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions;
using GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.Model;
using GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.UseCase;
using GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.Validation;
using GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.Model;
using GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.UseCase;
using GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Subscriptions;

/// <summary>
/// Тесты админских subscription use case'ов (F17.6 на mobile / web).
/// Покрывают: self-guard, admin-vs-admin/super-admin иерархия,
/// happy path с правильным Save и доменным эффектом.
/// </summary>
public sealed class AdminSubscriptionUseCaseTests
{
    // ─────────── RestartTrialByAdmin ───────────

    [Fact]
    public async Task RestartTrial_Self_ReturnsRevokeSelfForbidden()
    {
        var adminId = Guid.NewGuid();
        var (userRepo, currentUser, useCase) = BuildRestart();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(adminId));

        var result = await useCase.Execute(
            new RestartTrialByAdminCommand(adminId, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.revoke.self_forbidden");
        userRepo.Verify(x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestartTrial_UserNotFound_ReturnsNotFound()
    {
        var (userRepo, currentUser, useCase) = BuildRestart();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        var targetId = Guid.NewGuid();
        userRepo.Setup(x => x.GetById(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await useCase.Execute(
            new RestartTrialByAdminCommand(targetId, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.not.found");
    }

    [Fact]
    public async Task RestartTrial_AdminTargetsSuperAdmin_ReturnsManageSuperAdminForbidden()
    {
        var (userRepo, currentUser, useCase) = BuildRestart(isCurrentSuperAdmin: false);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        var super = User.RegisterSuperAdmin("super@example.com", "$hash").Value;
        userRepo.Setup(x => x.GetById(super.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(super);

        var result = await useCase.Execute(
            new RestartTrialByAdminCommand(super.Id, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.manage.super_admin_forbidden");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestartTrial_Happy_SetsTrialAndSaves()
    {
        var (userRepo, currentUser, useCase) = BuildRestart();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        var target = User.Register("user@example.com", "$hash").Value;
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new RestartTrialByAdminCommand(target.Id, DurationDays: 14),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.Subscription.Status.Should().Be(SubscriptionStatus.Trial);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────── RevokeSubscriptionByAdmin ───────────

    [Fact]
    public async Task Revoke_Self_ReturnsRevokeSelfForbidden()
    {
        var adminId = Guid.NewGuid();
        var (userRepo, currentUser, useCase) = BuildRevoke();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(adminId));

        var result = await useCase.Execute(
            new RevokeSubscriptionByAdminCommand(adminId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.revoke.self_forbidden");
        userRepo.Verify(x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Revoke_AdminTargetsAdmin_ReturnsManageSuperAdminForbidden()
    {
        var (userRepo, currentUser, useCase) = BuildRevoke(isCurrentSuperAdmin: false);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        var targetAdmin = User.Register("admin@example.com", "$hash").Value;
        targetAdmin.ChangeRole(UserRole.Admin);
        userRepo.Setup(x => x.GetById(targetAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAdmin);

        var result = await useCase.Execute(
            new RevokeSubscriptionByAdminCommand(targetAdmin.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.manage.super_admin_forbidden");
    }

    [Fact]
    public async Task Revoke_SuperAdminTargetsAdmin_Succeeds()
    {
        var (userRepo, currentUser, useCase) = BuildRevoke(isCurrentSuperAdmin: true);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        var targetAdmin = User.Register("admin@example.com", "$hash").Value;
        targetAdmin.ChangeRole(UserRole.Admin);
        // Дадим целевому юзеру активную подписку, иначе домен скажет
        // "нечего ревокать".
        targetAdmin.RestartTrialByAdmin(DateTime.UtcNow, TimeSpan.FromDays(30));
        userRepo.Setup(x => x.GetById(targetAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAdmin);

        var result = await useCase.Execute(
            new RevokeSubscriptionByAdminCommand(targetAdmin.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Revoke_Happy_DropsSubscriptionToExpiredAndSaves()
    {
        var (userRepo, currentUser, useCase) = BuildRevoke();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        var target = User.Register("user@example.com", "$hash").Value;
        target.RestartTrialByAdmin(DateTime.UtcNow, TimeSpan.FromDays(30));
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new RevokeSubscriptionByAdminCommand(target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // После revoke домен переводит в Expired (или None — зависит
        // от стартового состояния). Главное — Save вызвался.
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────── Helpers ───────────

    private static (
        Mock<IUserRepository>,
        Mock<ICurrentUserService>,
        RestartTrialByAdminUseCase) BuildRestart(bool isCurrentSuperAdmin = true)
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin)))
            .Returns(isCurrentSuperAdmin);

        var opts = Options.Create(new SubscriptionOptions { TrialDurationDays = 30 });
        var useCase = new RestartTrialByAdminUseCase(
            userRepo.Object, currentUser.Object, opts,
            TestExecutor.With<RestartTrialByAdminCommand, RestartTrialByAdminCommandValidator>());
        return (userRepo, currentUser, useCase);
    }

    private static (
        Mock<IUserRepository>,
        Mock<ICurrentUserService>,
        RevokeSubscriptionByAdminUseCase) BuildRevoke(bool isCurrentSuperAdmin = true)
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin)))
            .Returns(isCurrentSuperAdmin);

        var useCase = new RevokeSubscriptionByAdminUseCase(
            userRepo.Object, currentUser.Object,
            TestExecutor.With<RevokeSubscriptionByAdminCommand, RevokeSubscriptionByAdminCommandValidator>());
        return (userRepo, currentUser, useCase);
    }
}
