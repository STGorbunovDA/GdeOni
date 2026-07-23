namespace GdeOni.Application.Auth.ResetPassword.Model;

/// <summary>
/// D43. Установка нового пароля по токену из письма. Анонимная операция:
/// подтверждением личности служит сам токен.
/// </summary>
/// <param name="Token">Токен из ссылки в письме (открытый вид).</param>
/// <param name="NewPassword">Новый пароль.</param>
public sealed record ResetPasswordCommand(string Token, string NewPassword);
