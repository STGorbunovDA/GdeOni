using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Storage;

namespace GdeOni.Mobile.Services.Auth;

/// <summary>
/// При 401 Unauthorized пытается обновить пару access+refresh через
/// /api/auth/refresh и повторить исходный запрос один раз.
/// Refresh идёт через отдельный HttpClient, чтобы не уйти в рекурсию.
/// </summary>
public sealed class RefreshTokenHandler(
    ITokenStore tokenStore,
    IRefreshHttpClientProvider refreshClientProvider) : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshGate = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // Не пытаемся рефрешиться на самом /auth/refresh.
        if (request.RequestUri?.AbsolutePath.EndsWith("/auth/refresh", StringComparison.OrdinalIgnoreCase) == true)
            return response;

        var refreshed = await TryRefreshAsync(cancellationToken);
        if (!refreshed)
            return response;

        // Повторяем запрос с новым access.
        response.Dispose();
        var retry = await CloneRequestAsync(request);
        var newAccess = await tokenStore.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(newAccess))
            retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccess);
        return await base.SendAsync(retry, cancellationToken);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        await RefreshGate.WaitAsync(cancellationToken);
        try
        {
            var refresh = await tokenStore.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refresh))
                return false;

            var client = refreshClientProvider.Client;
            var response = await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshRequest(refresh),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await tokenStore.ClearAsync();
                return false;
            }

            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<RefreshResponse>>(
                cancellationToken: cancellationToken);
            if (envelope?.Result is null)
            {
                await tokenStore.ClearAsync();
                return false;
            }

            await tokenStore.SaveAsync(envelope.Result.AccessToken, envelope.Result.RefreshToken);
            return true;
        }
        finally
        {
            RefreshGate.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage source)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri)
        {
            Version = source.Version
        };

        if (source.Content is not null)
        {
            var ms = new MemoryStream();
            await source.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);
            foreach (var header in source.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in source.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}

/// <summary>
/// Изоляция HttpClient для refresh-запросов: чтобы RefreshTokenHandler не
/// рекурсивно уходил в сам себя через DI.
/// </summary>
public interface IRefreshHttpClientProvider
{
    HttpClient Client { get; }
}

public sealed class RefreshHttpClientProvider(IHttpClientFactory factory) : IRefreshHttpClientProvider
{
    public const string ClientName = "GdeOni.RefreshClient";

    public HttpClient Client => factory.CreateClient(ClientName);
}
