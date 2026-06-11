using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Complimentary.Commands.Grant.Model;
using GdeOni.Application.Complimentary.Commands.Grant.UseCase;
using GdeOni.Application.Complimentary.Commands.Grant.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Complimentary;

/// <summary>
/// D22. Тесты <see cref="GrantComplimentaryAccessUseCase"/>:
/// admin-проверки + доменный вызов + Save.
/// </summary>
public sealed class GrantComplimentaryAccessUseCaseTests
{
    [Fact]
    public async Task Execute_NotAdmin_ReturnsForbidden()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        var result = await useCase.Execute(
            new GrantComplimentaryAccessCommand(Guid.NewGuid(), null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Execute_GrantToSelf_ReturnsForbidden()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var adminId = Guid.NewGuid();
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(adminId));

        var result = await useCase.Execute(
            new GrantComplimentaryAccessCommand(adminId, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("complimentary.grant.self.forbidden");
    }

    [Fact]
    public async Task Execute_TargetUserNotFound_ReturnsNotFound()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(adminId));
        userRepo.Setup(x => x.GetById(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await useCase.Execute(
            new GrantComplimentaryAccessCommand(targetId, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Execute_AdminCannotManageSuperAdmin_ReturnsForbidden()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var adminId = Guid.NewGuid();
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(adminId));
        currentUser.Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin))).Returns(false);

        var target = User.RegisterSuperAdmin("super@example.com", "hash$hash$hash$hash").Value;
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new GrantComplimentaryAccessCommand(target.Id, null, "promo"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("complimentary.manage.super_admin.forbidden");
    }

    [Fact]
    public async Task Execute_HappyPath_CallsDomainAndSaves()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        var adminId = Guid.NewGuid();
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(adminId));

        var target = User.Register("user@example.com", "hash$hash$hash$hash").Value;
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new GrantComplimentaryAccessCommand(target.Id, UntilUtc: null, Note: "friend"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.HasComplimentaryAccess(DateTime.UtcNow).Should().BeTrue();
        target.ComplimentaryAccessGrantedByAdminId.Should().Be(adminId);
        target.ComplimentaryAccessNote.Should().Be("friend");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_EmptyTargetUserId_ReturnsValidationError()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        currentUser.Setup(x => x.IsAdmin()).Returns(true);

        var result = await useCase.Execute(
            new GrantComplimentaryAccessCommand(Guid.Empty, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<ICurrentUserService> CurrentUser,
        GrantComplimentaryAccessUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var executor = TestExecutor.With<GrantComplimentaryAccessCommand, GrantComplimentaryAccessCommandValidator>();
        var useCase = new GrantComplimentaryAccessUseCase(
            userRepo.Object, currentUser.Object, executor,
            new Mock<GdeOni.Application.Common.Security.ISecurityStampInvalidator>().Object,
            TimeProvider.System);
        return (userRepo, currentUser, useCase);
    }
}
