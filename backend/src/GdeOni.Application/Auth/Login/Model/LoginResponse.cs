namespace GdeOni.Application.Auth.Login.Model;

public sealed record LoginResponse(
    Guid Id,
    string Email,
    string UserName,
    string? FullName,
    string Role,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    // D45. Подтверждён ли email. false бывает только у «старых»
    // пользователей (новых до подтверждения гейт вообще не пускает) —
    // клиент по этому полю показывает баннер «подтвердите email».
    bool IsEmailConfirmed);
