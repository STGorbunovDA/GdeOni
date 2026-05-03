using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Common.Security;

public interface IJwtProvider
{
    AccessToken GenerateAccessToken(User user);
}
