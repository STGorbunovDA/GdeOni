using System.Security.Claims;
using GdeOni.Application.Common.Security;

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
}