namespace GdeOni.API.Models.Admin.ComplimentaryAccess;

/// <summary>
/// D22. Тело запроса <c>POST /api/admin/users/{userId}/complimentary-access</c>.
/// </summary>
/// <param name="UntilUtc">
/// До какого момента действует бесплатный доступ. null = бессрочно.
/// </param>
/// <param name="Note">
/// Опциональная заметка (≤500 символов) — например, "друг основателя"
/// или "support ticket 42".
/// </param>
public sealed record GrantComplimentaryAccessRequest(
    DateTime? UntilUtc,
    string? Note);
