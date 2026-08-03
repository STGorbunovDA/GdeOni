namespace GdeOni.Application.Events.Commands.UpdateCustomEvent.Model;

public sealed record UpdateCustomEventCommand(
    Guid Id,
    string Title,
    DateOnly Date,
    IReadOnlyList<int> LeadDays);
