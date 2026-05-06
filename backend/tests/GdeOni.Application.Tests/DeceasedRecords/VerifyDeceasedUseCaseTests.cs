using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.Verify.Model;
using GdeOni.Application.DeceasedRecords.Commands.Verify.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Verify.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты <see cref="VerifyDeceasedUseCase"/> — admin-only команда
/// постановки галочки IsVerified. Покрываем три ключевых случая:
/// non-admin → 403, повторная верификация → 409, happy path → 200.
/// </summary>
public sealed class VerifyDeceasedUseCaseTests
{
    /// <summary>
    /// Не-админ → VerifyForbidden. До GetById даже не доходит:
    /// чужие карточки в память не материализуются.
    /// </summary>
    [Fact]
    public async Task Execute_NonAdmin_ReturnsVerifyForbidden()
    {
        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);

        var useCase = new VerifyDeceasedUseCase(
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<VerifyDeceasedCommand, VerifyDeceasedCommandValidator>());

        var result = await useCase.Execute(
            new VerifyDeceasedCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.verify.forbidden");
        deceasedRepo.Verify(
            x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Повторная верификация на уже verified → AlreadyVerified (409).
    /// Domain-layer ловит, use case прокидывает.
    /// </summary>
    [Fact]
    public async Task Execute_AlreadyVerified_ReturnsAlreadyVerified()
    {
        // Arrange: создаём deceased и заранее верифицируем.
        var deceased = Deceased.Create(
            "Иван", "Иванов", null,
            null, new DateOnly(2010, 1, 1), null, Guid.NewGuid()).Value;
        deceased.Verify();

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo
            .Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new VerifyDeceasedUseCase(
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<VerifyDeceasedCommand, VerifyDeceasedCommandValidator>());

        var result = await useCase.Execute(
            new VerifyDeceasedCommand(deceased.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.already.verified");
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Happy path: admin → верифицирует свежую карточку, Save вызван,
    /// IsVerified = true в response.
    /// </summary>
    [Fact]
    public async Task Execute_AdminFreshDeceased_VerifiesAndSaves()
    {
        var deceased = Deceased.Create(
            "Иван", "Иванов", null,
            null, new DateOnly(2010, 1, 1), null, Guid.NewGuid()).Value;

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid()));
        currentUser.Setup(x => x.IsAdmin()).Returns(true);
        deceasedRepo
            .Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new VerifyDeceasedUseCase(
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<VerifyDeceasedCommand, VerifyDeceasedCommandValidator>());

        var result = await useCase.Execute(
            new VerifyDeceasedCommand(deceased.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsVerified.Should().BeTrue();
        deceased.IsVerified.Should().BeTrue();
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }
}
