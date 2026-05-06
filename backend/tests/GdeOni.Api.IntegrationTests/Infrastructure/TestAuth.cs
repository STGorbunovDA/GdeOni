using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using GdeOni.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using BCryptNet = BCrypt.Net.BCrypt;

namespace GdeOni.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Хелперы для D9.5.4: регистрация пользователя через API,
/// логин через API, создание авторизованного HttpClient.
/// Также — прямое создание admin-пользователя в БД (через API
/// нельзя — там Register отвергает Admin/SuperAdmin роли).
/// </summary>
internal static class TestAuth
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Создаёт нового HttpClient (без auto-redirect) для конкретного теста.
    /// </summary>
    public static HttpClient CreateClient(this GdeOniWebAppFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    /// <summary>
    /// Регистрирует нового RegularUser через POST /api/users.
    /// </summary>
    public static async Task<(string Email, string Password, string UserName, Guid Id)> RegisterAsync(
        this GdeOniWebAppFactory factory,
        HttpClient? client = null,
        string? userName = null)
    {
        client ??= factory.CreateClient();
        var email = $"int-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";
        userName ??= $"u{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            email,
            userName,
            password
        });
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponseDto<RegisterResultDto>>(JsonOptions);
        return (email, password, userName, payload!.Result!.Id);
    }

    /// <summary>
    /// Логин через API. Возвращает (access, refresh).
    /// </summary>
    public static async Task<(string Access, string Refresh)> LoginAsync(
        this HttpClient client,
        string email,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponseDto<TokenPairDto>>(JsonOptions);
        return (payload!.Result!.AccessToken, payload.Result.RefreshToken);
    }

    /// <summary>
    /// Регистрирует пользователя, логинит, ставит Authorization header.
    /// Возвращает (client с Bearer, userId, email, accessToken, refreshToken).
    /// </summary>
    public static async Task<AuthenticatedUser> RegisterAndLoginAsync(this GdeOniWebAppFactory factory)
    {
        var client = factory.CreateClient();
        var (email, password, userName, id) = await factory.RegisterAsync(client);
        var (access, refresh) = await client.LoginAsync(email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        return new AuthenticatedUser(client, id, email, password, userName, access, refresh);
    }

    /// <summary>
    /// Создаёт пользователя с заданной ролью напрямую в БД, обходя Register
    /// (он отвергает Admin/SuperAdmin). Используется в admin-сценариях.
    /// </summary>
    public static async Task<AuthenticatedUser> CreateAuthorizedUserWithRoleAsync(
        this GdeOniWebAppFactory factory,
        UserRole role)
    {
        const string password = "Password123!";
        var email = $"admin-{Guid.NewGuid():N}@example.com";
        var userName = $"adm{Guid.NewGuid():N}";

        Guid id;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hash = BCryptNet.HashPassword(password);
            User user = role == UserRole.SuperAdmin
                ? User.RegisterSuperAdmin(email, hash, userName: userName).Value
                : RegisterWithRole(email, hash, userName, role);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            id = user.Id;
        }

        var client = factory.CreateClient();
        var (access, refresh) = await client.LoginAsync(email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        return new AuthenticatedUser(client, id, email, password, userName, access, refresh);
    }

    private static User RegisterWithRole(string email, string hash, string userName, UserRole role)
    {
        // RegularUser идёт через обычный Register; иначе — Register допускает
        // Moderator (если такая существует), но Admin он отвергает. Для Admin
        // создаём как RegularUser, потом ChangeRole в Admin (через домен).
        if (role == UserRole.RegularUser)
            return User.Register(email, hash, userName: userName).Value;

        var user = User.Register(email, hash, userName: userName).Value;
        var change = user.ChangeRole(role);
        if (change.IsFailure)
            throw new InvalidOperationException($"ChangeRole({role}) failed: {change.Error.Code}");
        return user;
    }

    public sealed record AuthenticatedUser(
        HttpClient Client,
        Guid Id,
        string Email,
        string Password,
        string UserName,
        string AccessToken,
        string RefreshToken);

    private sealed class ApiResponseDto<TResult>
    {
        public TResult? Result { get; set; }
    }

    private sealed class RegisterResultDto
    {
        public Guid Id { get; set; }
    }

    private sealed class TokenPairDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
