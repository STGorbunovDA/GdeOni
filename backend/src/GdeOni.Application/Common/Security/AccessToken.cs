namespace GdeOni.Application.Common.Security;

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
