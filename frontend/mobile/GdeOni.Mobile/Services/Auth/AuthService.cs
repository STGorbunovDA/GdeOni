using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Observability;
using GdeOni.Mobile.Services.Storage;
using Refit;

namespace GdeOni.Mobile.Services.Auth;

public sealed class AuthService(
    IAuthApi authApi,
    IUsersApi usersApi,
    ITokenStore tokenStore,
    ISentryScopeService sentryScope) : IAuthService
{
    public async Task<bool> HasSessionAsync()
    {
        var token = await tokenStore.GetAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<AuthResult> LoginAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        try
        {
            var envelope = await authApi.LoginAsync(new LoginRequest(email, password), ct);
            if (envelope.Result is null)
                return new AuthResult(false, envelope.ErrorCode, envelope.ErrorMessage ?? "Неизвестная ошибка backend.");

            await tokenStore.SaveAsync(envelope.Result.AccessToken, envelope.Result.RefreshToken);
            // E25. Привязываем crash-репорты к userId (без email — 152-ФЗ
            // privacy-by-default).
            sentryScope.SetUser(envelope.Result.Id);
            return new AuthResult(true);
        }
        catch (ApiException apiEx)
        {
            return new AuthResult(false, $"http.{(int)apiEx.StatusCode}", ExtractErrorMessage(apiEx));
        }
        catch (HttpRequestException httpEx)
        {
            return new AuthResult(false, "network", $"Сетевая ошибка: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            return new AuthResult(false, "unknown", ex.Message);
        }
    }

    public async Task<AuthResult> RegisterAsync(
        string email,
        string? userName,
        string? fullName,
        string password,
        DateOnly birthDate,
        bool privacyPolicyAccepted,
        bool termsAccepted,
        CancellationToken ct = default)
    {
        try
        {
            // E24: чекбоксы 152-ФЗ показываются в RegisterPage; сюда
            // приходят реальные значения. Бэк-валидатор отвергнет
            // регистрацию если хоть один false (D19). D19-age:
            // BirthDate обязательный, проверка возраста 14+ — на бэке
            // через User.Register + TimeProvider.
            var registerEnv = await usersApi.RegisterAsync(
                new RegisterUserRequest(
                    email,
                    userName,
                    fullName,
                    password,
                    birthDate,
                    privacyPolicyAccepted,
                    termsAccepted), ct);
            if (registerEnv.Result is null)
                return new AuthResult(false, registerEnv.ErrorCode, registerEnv.ErrorMessage ?? "Регистрация не удалась.");

            // Auto-login после регистрации — UX лучше, чем форсить логин руками.
            return await LoginAsync(email, password, ct);
        }
        catch (ApiException apiEx)
        {
            return new AuthResult(false, $"http.{(int)apiEx.StatusCode}", ExtractErrorMessage(apiEx));
        }
        catch (HttpRequestException httpEx)
        {
            return new AuthResult(false, "network", $"Сетевая ошибка: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            return new AuthResult(false, "unknown", ex.Message);
        }
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        // E25. Сбрасываем Sentry-scope СНАЧАЛА — даже если backend logout
        // упадёт, дальнейшие crash'и не будут привязаны к ушедшему юзеру.
        sentryScope.ClearUser();

        var refresh = await tokenStore.GetRefreshTokenAsync();
        if (!string.IsNullOrEmpty(refresh))
        {
            try
            {
                await authApi.LogoutAsync(new LogoutRequest(refresh), ct);
            }
            catch
            {
                // Backend logout best-effort. Если сети нет / сервер упал —
                // всё равно чистим локальные токены ниже.
            }
        }

        await tokenStore.ClearAsync();
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        try
        {
            var envelope = await usersApi.GetCurrentAsync(ct);
            return envelope.Result;
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractErrorMessage(ApiException apiEx)
    {
        if (string.IsNullOrEmpty(apiEx.Content))
            return apiEx.ReasonPhrase ?? apiEx.StatusCode.ToString();

        // Backend всегда отвечает в формате ApiEnvelope; пытаемся достать
        // errorMessage. Если не вышло — возвращаем сырой текст ответа.
        try
        {
            var envelope = System.Text.Json.JsonSerializer.Deserialize<ApiEnvelope<object>>(
                apiEx.Content,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            return envelope?.ErrorMessage ?? apiEx.Content;
        }
        catch
        {
            return apiEx.Content;
        }
    }
}
