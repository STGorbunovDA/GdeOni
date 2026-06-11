using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.User;

// Partial-split от User.cs (god-class). F17.10 — блокировка аккаунта
// админом. Отдельный bounded context: вместе с Block/Unblock здесь
// живут поля состояния (IsBlocked, BlockedAtUtc, BlockedByUserId,
// BlockedReason — пока они в User.cs, но логически принадлежат сюда).
public sealed partial class User
{
    /// <summary>
    /// Блокировка аккаунта админом. Ротация SecurityStamp инвалидирует
    /// активную access-сессию (см. OnTokenValidated middleware). Reason
    /// опционален — для аудита и UX (юзер видит на login-экране).
    /// </summary>
    public UnitResult<Error> Block(Guid byAdminId, string? reason, DateTime nowUtc)
    {
        if (byAdminId == Guid.Empty)
            return Errors.User.AdminIdRequired();
        if (byAdminId == Id)
            return Errors.User.BlockSelfForbidden();

        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (normalizedReason is not null && normalizedReason.Length > MaxBlockReasonLength)
            return Errors.User.BlockReasonTooLong(MaxBlockReasonLength);

        // No-op guard: уже заблокирован той же причиной → не дёргаем
        // SecurityStamp/Touch, чтобы повторный POST был идемпотентен.
        if (IsBlocked && BlockedReason == normalizedReason && BlockedByUserId == byAdminId)
            return UnitResult.Success<Error>();

        IsBlocked = true;
        BlockedAtUtc = nowUtc;
        BlockedByUserId = byAdminId;
        BlockedReason = normalizedReason;
        SecurityStamp = Guid.NewGuid();
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Разблокировка. Юзер снова может логиниться, но активная сессия
    /// (если оставалась) уже была инвалидирована при Block — повторно
    /// ротировать SecurityStamp смысла нет.
    /// </summary>
    public UnitResult<Error> Unblock()
    {
        if (!IsBlocked) return UnitResult.Success<Error>();

        IsBlocked = false;
        BlockedAtUtc = null;
        BlockedByUserId = null;
        BlockedReason = null;
        Touch();
        return UnitResult.Success<Error>();
    }
}
