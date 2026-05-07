namespace GdeOni.Application.Common.Security;

/// <summary>
/// Сбрасывает закешированный SecurityStamp в API-слое (см. D11.8.1).
/// Use case'ы, ротирующие SecurityStamp у User (ChangeEmail / ChangePassword /
/// ChangeRole / UpdateProfile), вызывают Invalidate после Save —
/// иначе старый stamp в IMemoryCache держит уже выпущенные access-токены
/// валидными до истечения SecurityStampCacheTtlSeconds.
/// </summary>
public interface ISecurityStampInvalidator
{
    void Invalidate(Guid userId);
}
