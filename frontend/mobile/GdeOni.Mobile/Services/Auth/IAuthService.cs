using GdeOni.Mobile.Services.Api.Models;

namespace GdeOni.Mobile.Services.Auth;

public interface IAuthService
{
    /// <summary>
    /// True если есть валидный access-токен в SecureStorage. Не делает
    /// запрос на сервер — только проверка наличия. Полную проверку токена
    /// делает первый запрос к /api/users/me — если 401, RefreshTokenHandler
    /// попробует обновить, при провале сбросит.
    /// </summary>
    Task<bool> HasSessionAsync();

    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);

    Task<AuthResult> RegisterAsync(
        string email,
        string? userName,
        string? fullName,
        string password,
        CancellationToken ct = default);

    /// <summary>
    /// Чистит токены локально и пытается отозвать refresh на сервере.
    /// Серверный отзыв best-effort: если сеть упала — токены всё равно
    /// очищены и юзер вылогинен с устройства.
    /// </summary>
    Task LogoutAsync(CancellationToken ct = default);

    /// <summary>
    /// Возвращает данные текущего пользователя по существующему access-токену.
    /// </summary>
    Task<CurrentUserResponse?> GetCurrentUserAsync(CancellationToken ct = default);
}

public sealed record AuthResult(bool Success, string? ErrorCode = null, string? ErrorMessage = null);
