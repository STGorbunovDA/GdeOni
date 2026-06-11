namespace GdeOni.Application.Users.Commands.Register.Model;

/// <summary>
/// D19. <c>PrivacyPolicyAccepted</c> и <c>TermsAccepted</c> добавлены
/// для 152-ФЗ: регистрация без явного согласия с обоими документами
/// запрещена валидатором.
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string? UserName,
    string? FullName,
    string Password,
    bool PrivacyPolicyAccepted,
    bool TermsAccepted);
