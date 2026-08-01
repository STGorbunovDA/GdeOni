namespace GdeOni.Application.Users.Commands.Register.Model;

/// <summary>
/// D45. <paramref name="RequiresEmailConfirmation"/> = true, когда вход
/// этому юзеру будет закрыт до подтверждения email (гейт реально
/// применится: аккаунт под гейтом И почтовый канал настроен). Клиент по
/// нему решает: показать экран «проверьте почту» вместо авто-логина.
/// В dev без SMTP флаг = false — тогда клиент логинит сразу, как раньше.
/// </summary>
public sealed record RegisterUserResponse(Guid Id, bool RequiresEmailConfirmation);
