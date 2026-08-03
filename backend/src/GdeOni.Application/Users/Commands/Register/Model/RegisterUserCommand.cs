namespace GdeOni.Application.Users.Commands.Register.Model;

/// <summary>
/// D19. <c>PrivacyPolicyAccepted</c> и <c>TermsAccepted</c> добавлены
/// для 152-ФЗ: регистрация без явного согласия с обоими документами
/// запрещена валидатором. <c>BirthDate</c> — обязательное поле
/// после введения возрастного гарда (14 лет по Условиям использования,
/// п. 3.4).
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string? UserName,
    string? FullName,
    string Password,
    DateOnly BirthDate,
    bool PrivacyPolicyAccepted,
    bool TermsAccepted,
    // Функция «Родственники»: согласие быть видимым/получать сообщения.
    // По умолчанию true (маппер подставляет true, если клиент не прислал);
    // человек может снять галочку при регистрации.
    bool AllowRelativeConnections = true);
