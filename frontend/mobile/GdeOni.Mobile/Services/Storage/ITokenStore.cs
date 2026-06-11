namespace GdeOni.Mobile.Services.Storage;

public interface ITokenStore
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SaveAsync(string accessToken, string refreshToken);
    Task ClearAsync();
}
