using GdeOni.API.Models.Auth;
using GdeOni.API.Models.Users;
using GdeOni.Application.Auth.ForgotPassword.Model;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Application.Auth.Logout.Model;
using GdeOni.Application.Auth.Refresh.Model;
using GdeOni.Application.Auth.ResetPassword.Model;

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

    /// <summary>D43. Маппит DTO запроса ссылки восстановления.</summary>
    public static ForgotPasswordCommand ToCommand(this ForgotPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ForgotPasswordCommand(request.Email);
    }

    /// <summary>D43. Маппит DTO установки нового пароля по токену.</summary>
    public static ResetPasswordCommand ToCommand(this ResetPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ResetPasswordCommand(request.Token, request.NewPassword);
    }

    /// <summary>Маппит DTO выхода из системы в команду use case.</summary>
    public static LogoutCommand ToCommand(this LogoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new LogoutCommand(request.RefreshToken);
    }
}
