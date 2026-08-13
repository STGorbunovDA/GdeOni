namespace GdeOni.Application.Auth.Login.Model;

/// <summary>
/// Вход в систему. <paramref name="EmailOrLogin"/> — email ИЛИ логин:
/// пользователи часто вводят «псевдоним, под которым регистрировался»,
/// а не почту. В API-контракте поле по-прежнему называется <c>email</c>
/// (совместимость с уже выпущенными клиентами).
/// </summary>
public sealed record LoginCommand(string EmailOrLogin, string Password);
