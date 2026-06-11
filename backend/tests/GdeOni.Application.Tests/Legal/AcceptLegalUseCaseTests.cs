using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Legal;
using GdeOni.Application.Legal.Commands.AcceptLegal.Model;
using GdeOni.Application.Legal.Commands.AcceptLegal.UseCase;
using GdeOni.Application.Legal.Commands.AcceptLegal.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Legal;

/// <summary>
/// D19. Тесты <see cref="AcceptLegalUseCase"/>: маппинг через
/// CurrentUserService, проверка outdated-версий, обновление User
/// и Save.
/// </summary>
public sealed class AcceptLegalUseCaseTests
{
    [Fact]
    public async Task Execute_CurrentVersions_AcceptsAndSaves()
    {
        var (userRepo, currentUser, useCase) = BuildHarness(currentPrivacy: 1, currentTerms: 1);
        var user = User.Register("alice@example.com", "hash").Value;
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await useCase.Execute(
            new AcceptLegalCommand(PrivacyPolicyVersion: 1, TermsVersion: 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PrivacyPolicyVersion.Should().Be(1);
        user.TermsVersion.Should().Be(1);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_OutdatedPrivacyVersion_ReturnsConflict()
    {
        var (userRepo, currentUser, useCase) = BuildHarness(currentPrivacy: 2, currentTerms: 1);
        var user = User.Register("alice@example.com", "hash").Value;
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(user.Id));
        userRepo.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Клиент шлёт PrivacyPolicyVersion=1, но на сервере уже 2.
        var result = await useCase.Execute(
            new AcceptLegalCommand(PrivacyPolicyVersion: 1, TermsVersion: 1),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("legal.version.outdated");
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_UserNotFound_ReturnsForbidden()
    {
        var (userRepo, currentUser, useCase) = BuildHarness(currentPrivacy: 1, currentTerms: 1);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        userRepo.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await useCase.Execute(
            new AcceptLegalCommand(PrivacyPolicyVersion: 1, TermsVersion: 1),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.forbidden");
    }

    [Fact]
    public async Task Execute_InvalidPrivacyVersion_ReturnsValidationError()
    {
        var (userRepo, currentUser, useCase) = BuildHarness(currentPrivacy: 1, currentTerms: 1);
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));

        var result = await useCase.Execute(
            new AcceptLegalCommand(PrivacyPolicyVersion: 0, TermsVersion: 1),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<ICurrentUserService> CurrentUser,
        AcceptLegalUseCase UseCase) BuildHarness(int currentPrivacy, int currentTerms)
    {
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var options = Options.Create(new LegalOptions
        {
            CurrentPrivacyPolicyVersion = currentPrivacy,
            CurrentTermsVersion = currentTerms,
        });
        var useCase = new AcceptLegalUseCase(
            userRepo.Object,
            currentUser.Object,
            TestExecutor.With<AcceptLegalCommand, AcceptLegalCommandValidator>(),
            options,
            TimeProvider.System);
        return (userRepo, currentUser, useCase);
    }
}
