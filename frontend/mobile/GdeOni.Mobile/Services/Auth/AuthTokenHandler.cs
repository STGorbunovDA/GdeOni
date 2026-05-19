using System.Net.Http.Headers;
using GdeOni.Mobile.Services.Storage;

namespace GdeOni.Mobile.Services.Auth;

/// <summary>
/// Цепляет Authorization: Bearer {access} к каждому исходящему запросу.
/// Если токена нет — пропускает запрос как есть (на /auth/login это нормально).
/// </summary>
public sealed class AuthTokenHandler(ITokenStore tokenStore) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var access = await tokenStore.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(access))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
