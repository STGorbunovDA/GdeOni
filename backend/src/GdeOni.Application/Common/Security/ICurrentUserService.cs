namespace GdeOni.Application.Common.Security;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(params string[] roles);
}