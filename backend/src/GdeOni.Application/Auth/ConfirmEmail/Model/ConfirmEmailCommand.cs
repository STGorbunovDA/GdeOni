namespace GdeOni.Application.Auth.ConfirmEmail.Model;

/// <summary>
/// D45. Подтверждение адреса email по токену из письма. Анонимная
/// операция: подтверждением служит сам токен.
/// </summary>
/// <param name="Token">Токен из ссылки в письме (открытый вид).</param>
public sealed record ConfirmEmailCommand(string Token);
