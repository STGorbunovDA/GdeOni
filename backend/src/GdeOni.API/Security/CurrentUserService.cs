using System.Security.Claims;
using CSharpFunctionalExtensions;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;

namespace GdeOni.API.Security;

/// <summary>
/// HTTP-реализация <see cref="ICurrentUserService"/>: достаёт идентификатор
/// и роль текущего пользователя из claim'ов <see cref="HttpContext.User"/>.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    /// <summary>Идентификатор текущего пользователя из claim'а NameIdentifier (null если не аутентифицирован).</summary>
    public Guid? UserId
    {
        get
        {
            var raw = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    /// <summary>Запрос аутентифицирован (есть валидный bearer-токен).</summary>
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    /// <summary>Возвращает true, если текущий пользователь принадлежит хотя бы одной из перечисленных ролей.</summary>
    public bool IsInRole(params string[] roles)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null)
            return false;

        return roles.Any(user.IsInRole);
    }

    /// <summary>Текущий пользователь — администратор (роль SuperAdmin или Admin).</summary>
    public bool IsAdmin() =>
        IsInRole(UserRole.SuperAdmin.ToString(), UserRole.Admin.ToString());

    /// <summary>D44. Только владелец сервиса, без обычных админов.</summary>
    public bool IsSuperAdmin() =>
        IsInRole(UserRole.SuperAdmin.ToString());

    /// <summary>
    /// Возвращает идентификатор текущего пользователя как
    /// <see cref="Result{T, E}"/>; при отсутствии аутентификации —
    /// ошибку <c>Errors.General.Unauthorized()</c>.
    /// </summary>
    public Result<Guid, Error> GetCurrentUserId()
    {
        if (!IsAuthenticated || !UserId.HasValue)
            return Errors.General.Unauthorized();

        return Result.Success<Guid, Error>(UserId.Value);
    }

    /// <summary>IP-адрес клиента (после обработки ForwardedHeaders).</summary>
    public string? GetRemoteIpAddress() =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
