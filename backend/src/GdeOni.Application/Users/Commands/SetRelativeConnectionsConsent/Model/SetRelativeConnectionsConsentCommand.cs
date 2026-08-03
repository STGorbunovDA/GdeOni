namespace GdeOni.Application.Users.Commands.SetRelativeConnectionsConsent.Model;

/// <summary>
/// Функция «Родственники»: включить/выключить согласие текущего пользователя
/// быть видимым как родственник другим отслеживающим ту же карточку и
/// получать от них сообщения.
/// </summary>
public sealed record SetRelativeConnectionsConsentCommand(bool Allow);
