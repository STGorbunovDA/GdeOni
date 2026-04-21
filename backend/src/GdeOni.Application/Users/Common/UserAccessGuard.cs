using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Common;

internal static class UserAccessGuard
{
    public static Error? EnsureCanAccessUser(
        Guid targetUserId,
        ICurrentUserService currentUserService)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return Error.Unauthorized("auth.unauthorized", "Authentication is required.");
        }

        var currentUserId = currentUserService.UserId.Value;
        var isAdmin = currentUserService.IsInRole(
            UserRole.SuperAdmin.ToString(),
            UserRole.Admin.ToString());

        if (!isAdmin && currentUserId != targetUserId)
        {
            return Error.Forbidden(
                "auth.forbidden",
                "You do not have access to this user resource.");
        }

        return null;
    }
}