namespace GdeOni.Application.Common.Security;

public interface IRefreshTokenFactory
{
    string Generate();

    string Hash(string token);
}
