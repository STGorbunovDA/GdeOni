namespace GdeOni.Mobile.Services.Api.Models;

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record LoginResponse(
    Guid Id,
    string Email,
    string UserName,
    string? FullName,
    string Role,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed record RefreshResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed record RegisterUserRequest(
    string Email,
    string? UserName,
    string? FullName,
    string Password);

public sealed record RegisterUserResponse(Guid Id);

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string UserName,
    string? FullName,
    string Role);

/// <summary>
/// Body для PUT /api/users/{id}/password. CurrentPassword обязателен,
/// когда юзер меняет свой пароль; nullable в API контракте, потому что
/// админ может менять чужой пароль без него.
/// </summary>
public sealed record ChangePasswordRequest(
    string? CurrentPassword,
    string NewPassword);

public sealed record ChangePasswordResponse(Guid UserId);

// Алиас для совместимости старого кода mobile-проекта. Источник истины —
// GdeOni.Mobile.Shared.Auth.PasswordPolicy (вынесено для юнит-тестов).
public static class PasswordPolicy
{
    public const int MinPasswordLength = Shared.Auth.PasswordPolicy.MinPasswordLength;
    public const int MaxPasswordLength = Shared.Auth.PasswordPolicy.MaxPasswordLength;
}
