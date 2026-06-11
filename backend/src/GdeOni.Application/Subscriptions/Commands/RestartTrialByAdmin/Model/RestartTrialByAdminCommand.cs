namespace GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.Model;

/// <summary>
/// Админский restart триала для конкретного юзера. DurationDays
/// опционально (default 30 — из SubscriptionOptions).
/// </summary>
public sealed record RestartTrialByAdminCommand(Guid UserId, int? DurationDays);
