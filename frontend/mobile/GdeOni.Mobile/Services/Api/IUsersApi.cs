using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

public interface IUsersApi
{
    [Post("/api/users")]
    Task<ApiEnvelope<RegisterUserResponse>> RegisterAsync(
        [Body] RegisterUserRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/users/me")]
    Task<ApiEnvelope<CurrentUserResponse>> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Смена пароля. После успеха backend ротирует SecurityStamp —
    /// текущий access-токен перестаёт работать (OnTokenValidated
    /// сверит и упадёт на 401). Поэтому мобилка после 200 OK должна
    /// сразу LogoutAsync() и отправить юзера на login.
    /// </summary>
    [Put("/api/users/{userId}/password")]
    Task<ApiEnvelope<ChangePasswordResponse>> ChangePasswordAsync(
        Guid userId,
        [Body] ChangePasswordRequest request,
        CancellationToken cancellationToken = default);
}
