namespace GdeOni.Application.Legal.Commands.AcceptLegal.Model;

/// <summary>
/// D19. Команда подтверждения принятия Privacy Policy и Terms of Use
/// на указанных версиях. Клиент берёт текущие версии из
/// <c>GET /api/legal/privacy-policy</c> и <c>GET /api/legal/terms-of-use</c>
/// и шлёт их обратно вместе с подтверждением.
/// </summary>
public sealed record AcceptLegalCommand(
    int PrivacyPolicyVersion,
    int TermsVersion);
