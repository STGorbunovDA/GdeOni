namespace GdeOni.Mobile.Services.Api.Models;

/// <summary>
/// E22. <c>GET /api/users/me/subscription</c>. Зеркало серверного DTO
/// (см. D16 + D22). UI на основании этих полей:
/// — если HasComplimentaryAccess=true → блок "Бесплатный доступ от
///   администратора" с UntilUtc/Note, кнопок Оформить/Отменить нет;
/// — иначе обычный flow Trial / Active / Cancelled / Expired.
/// </summary>
public sealed record MySubscriptionResponse(
    string Status,
    string? Plan,
    DateTime? ExpiresAtUtc,
    DateTime? CancelledAtUtc,
    bool IsActiveNow,
    bool IsOnTrial,
    int DaysUntilExpiry,
    bool HasComplimentaryAccess,
    DateTime? ComplimentaryAccessUntilUtc,
    string? ComplimentaryAccessNote);

/// <summary>
/// E22. Body для <c>POST /api/users/me/subscription/create-payment</c>.
/// <see cref="Platform"/> = "Mobile" — бэк вернёт нас через
/// deep-link <c>gdeoni://payment/return</c> (см. D16
/// SubscriptionOptions.MobileReturnUrl).
/// </summary>
public sealed record CreatePaymentRequest(string Plan, string Platform = "Mobile");

/// <summary>
/// E22. Ответ create-payment — URL для открытия в браузере / WebView.
/// </summary>
public sealed record CreatePaymentResponse(
    string CheckoutUrl,
    string ExternalPaymentId);
