using System.Security.Claims;
using CSharpFunctionalExtensions;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;

namespace GdeOni.API.Security;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var raw = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool IsInRole(params string[] roles)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null)
            return false;

        return roles.Any(user.IsInRole);
    }

    public bool IsAdmin() =>
        IsInRole(UserRole.SuperAdmin.ToString(), UserRole.Admin.ToString());

    public Result<Guid, Error> GetCurrentUserId()
    {
        if (!IsAuthenticated || !UserId.HasValue)
            return Errors.General.Unauthorized();

        return Result.Success<Guid, Error>(UserId.Value);
    }

    public string? GetRemoteIpAddress() =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}