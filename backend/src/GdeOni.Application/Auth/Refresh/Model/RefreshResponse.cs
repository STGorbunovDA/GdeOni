namespace GdeOni.Application.Auth.Refresh.Model;

public sealed record RefreshResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
