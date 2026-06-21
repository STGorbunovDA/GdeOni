using GdeOni.API.Models.Auth;
using GdeOni.API.Models.Users;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Application.Auth.Logout.Model;
using GdeOni.Application.Auth.Refresh.Model;

namespace GdeOni.API.Mappers;

/// <summary>
/// Request → Command маппинг для эндпоинтов аутентификации
/// (login/refresh/logout).
/// </summary>
public static class AuthMapping
{
    /// <summary>Маппит DTO входа в систему в команду use case.</summary>
    public static LoginCommand ToCommand(this LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new LoginCommand(request.Email, request.Password);
    }

    /// <summary>Маппит DTO refresh-токена в команду use case.</summary>
    public static RefreshCommand ToCommand(this RefreshRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new RefreshCommand(request.RefreshToken);
    }

    /// <summary>Маппит DTO выхода из системы в команду use case.</summary>
    public static LogoutCommand ToCommand(this LogoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new LogoutCommand(request.RefreshToken);
    }
}
