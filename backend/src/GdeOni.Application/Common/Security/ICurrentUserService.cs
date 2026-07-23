using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Common.Security;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(params string[] roles);
    bool IsAdmin();

    /// <summary>
    /// D44. Только владелец сервиса (роль SuperAdmin), без обычных
    /// админов. Введено для обращений: переписка идёт про оплату и
    /// содержит платёжные договорённости, поэтому доступ к ней уже
    /// не «любой администратор».
    /// </summary>
    bool IsSuperAdmin();
    Result<Guid, Error> GetCurrentUserId();
    string? GetRemoteIpAddress();
}