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
}
