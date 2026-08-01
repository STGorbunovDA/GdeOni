namespace GdeOni.Application.Sharing.Commands.CreateShareBundle.Model;

/// <summary>
/// D46. Ответ создания подборки: короткий код и срок действия. Полную
/// ссылку и QR клиент строит сам от своего origin (<c>{origin}/s/{code}</c>).
/// </summary>
public sealed record CreateShareBundleResponse(string Code, DateTime ExpiresAtUtc);
