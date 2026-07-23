namespace GdeOni.Application.Auth.ForgotPassword.Model;

/// <summary>
/// D43. Запрос ссылки восстановления пароля. Анонимный — юзер по
/// определению не может войти.
/// </summary>
/// <param name="Email">Адрес, на который отправить ссылку.</param>
public sealed record ForgotPasswordCommand(string Email);
