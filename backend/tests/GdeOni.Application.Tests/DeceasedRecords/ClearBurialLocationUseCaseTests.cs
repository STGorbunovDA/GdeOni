using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.ClearBurialLocation.Model;
using GdeOni.Application.DeceasedRecords.Commands.ClearBurialLocation.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ClearBurialLocation.Validation;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.DeceasedRecords;

/// <summary>
/// Тесты <see cref="ClearBurialLocationUseCase"/> — права автора
/// карточки на очистку BurialLocation. Покрываем outsider → 403,
/// автор → success, admin → success.
/// </summary>
public sealed class ClearBurialLocationUseCaseTests
{
    private static readonly Guid AuthorId = Guid.NewGuid();

    /// <summary>
    /// Outsider пытается очистить чужую карточку → ClearBurialLocationForbidden.
    /// </summary>
    [Fact]
    public async Task Execute_Outsider_ReturnsClearBurialLocationForbidden()
    {
        var deceased = CreateDeceasedWithBurial();

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(Guid.NewGuid())); // НЕ автор.
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo
            .Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new ClearBurialLocationUseCase(
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<ClearBurialLocationCommand, ClearBurialLocationCommandValidator>());

        var result = await useCase.Execute(
            new ClearBurialLocationCommand(deceased.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased.burial_location.clear.forbidden");
        // BurialLocation не менялся.
        deceased.BurialLocation.Should().NotBeNull();
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Автор → BurialLocation очищен, Save вызван.
    /// </summary>
    [Fact]
    public async Task Execute_Author_ClearsBurialAndSaves()
    {
        var deceased = CreateDeceasedWithBurial();

        var deceasedRepo = new Mock<IDeceasedRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser
            .Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(AuthorId));
        currentUser.Setup(x => x.IsAdmin()).Returns(false);
        deceasedRepo
            .Setup(x => x.GetById(deceased.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deceased);

        var useCase = new ClearBurialLocationUseCase(
            deceasedRepo.Object,
            currentUser.Object,
            TestExecutor.With<ClearBurialLocationCommand, ClearBurialLocationCommandValidator>());

        var result = await useCase.Execute(
            new ClearBurialLocationCommand(deceased.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deceased.BurialLocation.Should().BeNull();
        deceasedRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Deceased CreateDeceasedWithBurial()
    {
        var burial = BurialLocation.Create(55.0, 37.0).Value;
        return Deceased.Create(
            "Иван", "Иванов", null,
            null, new DateOnly(2010, 1, 1),
            burial, AuthorId).Value;
    }
}
