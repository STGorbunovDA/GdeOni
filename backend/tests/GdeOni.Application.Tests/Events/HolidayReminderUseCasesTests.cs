using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Events.Commands.SetHolidayReminder.Model;
using GdeOni.Application.Events.Commands.SetHolidayReminder.UseCase;
using GdeOni.Application.Events.Commands.SetHolidayReminder.Validation;
using GdeOni.Application.Events.Queries.GetMyHolidayReminders.UseCase;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.Events;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Tests.Events;

/// <summary>
/// Тесты use case'ов напоминаний о праздниках: upsert (создание/обновление),
/// отключение пустым набором, валидация допустимых «за сколько дней», чтение.
/// </summary>
public sealed class HolidayReminderUseCasesTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Set_NoExisting_CreatesAndSaves()
    {
        var (currentUser, repo) = Mocks();
        repo.Setup(x => x.GetByUserAndKey(UserId, "Пасха", It.IsAny<CancellationToken>()))
            .ReturnsAsync((HolidayReminder?)null);

        var useCase = BuildSet(currentUser, repo);

        var result = await useCase.Execute(
            new SetHolidayReminderCommand("Пасха", new[] { 0, 3 }), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LeadDays.Should().Equal(0, 3);
        repo.Verify(x => x.Add(
            It.Is<HolidayReminder>(r => r.UserId == UserId && r.HolidayKey == "Пасха"),
            It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Set_Existing_UpdatesLeadDays_NoInsert()
    {
        var existing = HolidayReminder.Create(UserId, "Радоница", new[] { 0 }, DateTime.UtcNow);
        var (currentUser, repo) = Mocks();
        repo.Setup(x => x.GetByUserAndKey(UserId, "Радоница", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var useCase = BuildSet(currentUser, repo);

        var result = await useCase.Execute(
            new SetHolidayReminderCommand("Радоница", new[] { 7, 1 }), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.LeadDays.Should().Equal(1, 7); // нормализовано (сортировка)
        repo.Verify(x => x.Add(It.IsAny<HolidayReminder>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Set_EmptyLeadDays_DisablesReminder()
    {
        var (currentUser, repo) = Mocks();
        repo.Setup(x => x.GetByUserAndKey(UserId, "Радоница", It.IsAny<CancellationToken>()))
            .ReturnsAsync((HolidayReminder?)null);

        var useCase = BuildSet(currentUser, repo);

        var result = await useCase.Execute(
            new SetHolidayReminderCommand("Радоница", Array.Empty<int>()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LeadDays.Should().BeEmpty();
    }

    [Fact]
    public async Task Set_InvalidLeadDay_FailsValidation_AndDoesNotSave()
    {
        var (currentUser, repo) = Mocks();
        var useCase = BuildSet(currentUser, repo);

        // 2 дня — не из допустимого набора {0,1,3,7}.
        var result = await useCase.Execute(
            new SetHolidayReminderCommand("Пасха", new[] { 2 }), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        repo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Get_ReturnsUserReminders()
    {
        var (currentUser, repo) = Mocks();
        var r1 = HolidayReminder.Create(UserId, "Пасха", new[] { 0, 7 }, DateTime.UtcNow);
        repo.Setup(x => x.GetByUser(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { r1 });

        var useCase = new GetMyHolidayRemindersUseCase(currentUser.Object, repo.Object);

        var result = await useCase.Execute(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Reminders.Should().ContainSingle();
        result.Value.Reminders[0].HolidayKey.Should().Be("Пасха");
        result.Value.Reminders[0].LeadDays.Should().Equal(0, 7);
    }

    private static (Mock<ICurrentUserService>, Mock<IHolidayReminderRepository>) Mocks()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserId())
            .Returns(Result.Success<Guid, Error>(UserId));
        var repo = new Mock<IHolidayReminderRepository>();
        return (currentUser, repo);
    }

    private static SetHolidayReminderUseCase BuildSet(
        Mock<ICurrentUserService> currentUser,
        Mock<IHolidayReminderRepository> repo) =>
        new(
            currentUser.Object,
            repo.Object,
            TestExecutor.With<SetHolidayReminderCommand, SetHolidayReminderCommandValidator>(),
            TimeProvider.System);
}
