namespace GdeOni.Mobile.Services.Api.Models;

/// <summary>F17.9 mobile. Ответ GET /api/admin/edits.</summary>
public sealed record AllEditsResponse(
    IReadOnlyList<AllEditsItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AllEditsItem(
    Guid Id,
    DateTime EditedAtUtc,
    Guid DeceasedId,
    string DeceasedFullName,
    Guid? EditedByUserId,
    string? EditedByEmail,
    string? EditedByDisplayName,
    string Kind,
    string ChangesJson);

/// <summary>F17.9 mobile. Ответ GET /api/users.</summary>
public sealed record AdminUsersResponse(
    IReadOnlyList<AdminUserListItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminUserListItem(
    Guid Id,
    string Email,
    string UserName,
    string? FullName,
    string Role,
    DateTime RegisteredAtUtc,
    DateTime? LastLoginAtUtc,
    int TrackingCount);

/// <summary>F17.9 mobile. Детали конкретного юзера + статус подписки.</summary>
public sealed record AdminUserDetailsDto(
    Guid Id,
    string Email,
    string UserName,
    string? FullName,
    string Role,
    DateTime RegisteredAtUtc,
    DateTime? LastLoginAtUtc,
    int TrackingCount,
    string SubscriptionStatus,
    DateTime? SubscriptionExpiresAtUtc,
    string? SubscriptionPlan,
    bool HasComplimentaryAccess,
    DateTime? ComplimentaryAccessUntilUtc,
    string? ComplimentaryAccessNote);

/// <summary>
/// PUT /api/users/{id}/role — бэк ожидает поле userRole с enum-строкой
/// (JsonStringEnumConverter): RegularUser / Manager / Admin / SuperAdmin.
/// </summary>
public sealed record ChangeRoleRequest(string UserRole);

/// <summary>POST /api/admin/users/{id}/subscription/trial.</summary>
public sealed record RestartTrialRequest(int? DurationDays);

/// <summary>GET /api/admin/users/{id}/tracked-deceased — список отслеживаний.</summary>
public sealed record AdminUserTrackedResponse(
    IReadOnlyList<AdminUserTrackedItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminUserTrackedItem(
    Guid DeceasedId,
    string FullName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    string RelationshipType,
    string Status,
    DateTime TrackedAtUtc);

/// <summary>Ответ на DELETE all-tracking.</summary>
public sealed record RemoveAllTrackingResponse(int RemovedCount);

public sealed record GrantComplimentaryRequest(DateTime? UntilUtc, string? Note);

/// <summary>D23. Ответ GET /api/admin/payments.</summary>
public sealed record AdminPaymentsResponse(
    IReadOnlyList<AdminPaymentItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminPaymentItem(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    string ExternalPaymentId,
    string Plan,
    decimal AmountRub,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? PeriodStartUtc,
    DateTime? PeriodEndUtc);
