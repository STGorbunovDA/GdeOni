namespace GdeOni.Application.Auth.Login.Model;

public sealed record LoginResponse(
    Guid Id,
    string Email,
    string UserName,
    string? FullName,
    string Role,
    string AccessToken);