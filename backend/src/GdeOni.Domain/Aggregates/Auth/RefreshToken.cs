using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Auth;

public sealed class RefreshToken : Entity<Guid>
{
    public const int TokenHashLength = 64;
    public const int MaxIpLength = 64;

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public string? CreatedFromIp { get; }

    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;
    public bool IsActive(DateTime nowUtc) => !IsRevoked && !IsExpired(nowUtc);

    private RefreshToken() : base(Guid.Empty)
    {
        TokenHash = null!;
    }

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime createdAtUtc,
        string? createdFromIp) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        CreatedFromIp = createdFromIp;
    }

    public static Result<RefreshToken, Error> Issue(
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime nowUtc,
        string? createdFromIp = null)
    {
        if (userId == Guid.Empty)
            return Errors.General.ValueIsRequired("userId");

        if (string.IsNullOrWhiteSpace(tokenHash))
            return Errors.RefreshToken.TokenHashRequired();

        if (expiresAtUtc <= nowUtc)
            return Errors.RefreshToken.TokenExpiresInPast();

        if (createdFromIp is { Length: > MaxIpLength })
            return Errors.RefreshToken.IpTooLong(MaxIpLength);

        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            expiresAtUtc,
            nowUtc,
            createdFromIp);
    }

    public UnitResult<Error> Revoke(DateTime nowUtc)
    {
        if (IsRevoked)
            return Errors.RefreshToken.TokenAlreadyRevoked();

        RevokedAtUtc = nowUtc;
        return UnitResult.Success<Error>();
    }
}
