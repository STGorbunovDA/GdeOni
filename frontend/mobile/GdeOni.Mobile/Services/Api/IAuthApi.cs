using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<ApiEnvelope<LoginResponse>> LoginAsync(
        [Body] LoginRequest request,
        CancellationToken cancellationToken = default);

    [Post("/api/auth/refresh")]
    Task<ApiEnvelope<RefreshResponse>> RefreshAsync(
        [Body] RefreshRequest request,
        CancellationToken cancellationToken = default);

    [Post("/api/auth/logout")]
    Task LogoutAsync(
        [Body] LogoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// D43. Просит отправить на email ссылку для смены пароля. Сама
    /// смена происходит на сайте — ссылка из письма ведёт на
    /// gdeoni.ru/reset-password. В приложении экрана ввода нового
    /// пароля нет намеренно: иначе пришлось бы заставлять человека
    /// переписывать токен из письма руками.
    /// </summary>
    [Post("/api/auth/forgot-password")]
    Task ForgotPasswordAsync(
        [Body] ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);
}
