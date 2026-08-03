using GdeOni.Domain.Aggregates.Events;

namespace GdeOni.Domain.Tests.EventsAggregate;

/// <summary>
/// Ручное событие: заголовок обязателен и ограничен, lead-days фильтруются до
/// разрешённых (0/1/3/7), Update идемпотентен при тех же значениях.
/// </summary>
public sealed class CustomEventTests
{
    private static readonly Guid User = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 9, 15);
    private static readonly DateTime Now = new(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_Valid_TrimsTitle_FiltersLeadDays()
    {
        var result = CustomEvent.Create(User, "  ДР друга  ", Date, new[] { 0, 3, 99, 3 }, Now);

        result.IsSuccess.Should().BeTrue();
        var ev = result.Value;
        ev.Title.Should().Be("ДР друга");
        ev.EventDate.Should().Be(Date);
        ev.LeadDays.Should().Equal(0, 3); // 99 отфильтровано, дубль убран, сортировка
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTitle_Rejected(string title)
    {
        var result = CustomEvent.Create(User, title, Date, new[] { 0 }, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("event.title.required");
    }

    [Fact]
    public void Create_TooLongTitle_Rejected()
    {
        var title = new string('x', CustomEvent.MaxTitleLength + 1);

        var result = CustomEvent.Create(User, title, Date, new[] { 0 }, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("event.title.too_long");
    }

    [Fact]
    public void Update_EmptyLeadDays_DisablesReminder()
    {
        var ev = CustomEvent.Create(User, "событие", Date, new[] { 0 }, Now).Value;

        var result = ev.Update("событие", Date, Array.Empty<int>(), Now.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        ev.LeadDays.Should().BeEmpty();
    }

    [Fact]
    public void Update_SameValues_NoOp_KeepsUpdatedAt()
    {
        var ev = CustomEvent.Create(User, "событие", Date, new[] { 0, 7 }, Now).Value;
        var before = ev.UpdatedAtUtc;

        var result = ev.Update("  событие ", Date, new[] { 7, 0 }, Now.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        ev.UpdatedAtUtc.Should().Be(before); // no-op: не двигаем таймстамп
    }
}
