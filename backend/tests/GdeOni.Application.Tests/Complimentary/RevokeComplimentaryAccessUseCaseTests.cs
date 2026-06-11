using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Complimentary.Commands.Revoke.Model;
using GdeOni.Application.Complimentary.Commands.Revoke.UseCase;
using GdeOni.Application.Complimentary.Commands.Revoke.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Complimentary;

/// <summary>
/// D22. Тесты <see cref="RevokeComplimentaryAccessUseCase"/>.
/// </summary>
public sealed class RevokeComplimentaryAccessUseCaseTests
{
    [Fact]
    public async Task Execute_NotAdmin_ReturnsForbidden()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        var result = await useCase.Execute(
            new RevokeComplimentaryAccessCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Execute_AdminCannotManageSuperAdmin_ReturnsForbidden()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        currentUser.Setup(x => x.IsInRole(nameof(UserRole.SuperAdmin))).Returns(false);

        var target = User.RegisterSuperAdmin("super@example.com", "hash$hash$hash$hash").Value;
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new RevokeComplimentaryAccessCommand(target.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("complimentary.manage.super_admin.forbidden");
    }

    [Fact]
    public async Task Execute_HappyPath_ClearsAccessAndSaves()
    {
        var (userRepo, currentUser, useCase) = BuildHarness();
        currentUser.Setup(x => x.IsAdmin()).Returns(true);

        var target = User.Register("user@example.com", "hash$hash$hash$hash").Value;
        target.GrantComplimentaryAccess(Guid.NewGuid(), null, "promo", DateTime.UtcNow);
        userRepo.Setup(x => x.GetById(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        var result = await useCase.Execute(
            new RevokeComplimentaryAccessCommand(target.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.HasComplimentaryAccess(DateTime.UtcNow).Should().BeFalse();
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<ICurrentUserService> CurrentUser,
        RevokeComplimentaryAccessUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var executor = TestExecutor.With<RevokeComplimentaryAccessCommand, RevokeComplimentaryAccessCommandValidator>();
        var useCase = new RevokeComplimentaryAccessUseCase(
            userRepo.Object, currentUser.Object, executor,
            new Mock<GdeOni.Application.Common.Security.ISecurityStampInvalidator>().Object);
        return (userRepo, currentUser, useCase);
    }
}
