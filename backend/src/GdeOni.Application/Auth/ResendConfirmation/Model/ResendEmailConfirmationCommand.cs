namespace GdeOni.Application.Auth.ResendConfirmation.Model;

/// <summary>
/// D45. Повторная отправка письма с подтверждением email. Анонимный —
/// зовётся и с экрана «проверьте почту» (новый юзер ещё не вошёл), и из
/// внутреннего баннера (там клиент подставляет email текущего юзера сам).
/// </summary>
/// <param name="Email">Адрес, на который отправить письмо.</param>
public sealed record ResendEmailConfirmationCommand(string Email);
