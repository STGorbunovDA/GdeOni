namespace GdeOni.Application.Events.Commands.CreateCustomEvent.Model;

/// <summary>Создать ручное событие. LeadDays — «за сколько дней» (0/1/3/7).</summary>
public sealed record CreateCustomEventCommand(
    string Title,
    DateOnly Date,
    IReadOnlyList<int> LeadDays);

public sealed record CreateCustomEventResponse(Guid Id);
