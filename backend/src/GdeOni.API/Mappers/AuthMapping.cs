using GdeOni.API.Models.Auth;
using GdeOni.API.Models.Users;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Application.Auth.Logout.Model;
using GdeOni.Application.Auth.Refresh.Model;

namespace GdeOni.API.Mappers;

public static class AuthMapping
{
    public static LoginCommand ToCommand(this LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new LoginCommand(request.Email, request.Password);
    }

    public static RefreshCommand ToCommand(this RefreshRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new RefreshCommand(request.RefreshToken);
    }

    public static LogoutCommand ToCommand(this LogoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new LogoutCommand(request.RefreshToken);
    }
}
