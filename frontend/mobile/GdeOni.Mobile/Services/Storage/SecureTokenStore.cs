namespace GdeOni.Mobile.Services.Storage;

public sealed class SecureTokenStore : ITokenStore
{
    private const string AccessKey = "gdeoni.access_token";
    private const string RefreshKey = "gdeoni.refresh_token";

    public Task<string?> GetAccessTokenAsync() =>
        SecureStorage.Default.GetAsync(AccessKey);

    public Task<string?> GetRefreshTokenAsync() =>
        SecureStorage.Default.GetAsync(RefreshKey);

    public async Task SaveAsync(string accessToken, string refreshToken)
    {
        await SecureStorage.Default.SetAsync(AccessKey, accessToken);
        await SecureStorage.Default.SetAsync(RefreshKey, refreshToken);
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.RemoveAll();
        return Task.CompletedTask;
    }
}
