using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Common.Security;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(params string[] roles);
    bool IsAdmin();
    Result<Guid, Error> GetCurrentUserId();
    string? GetRemoteIpAddress();
}