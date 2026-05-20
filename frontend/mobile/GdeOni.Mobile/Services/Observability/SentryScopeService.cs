using Sentry;

namespace GdeOni.Mobile.Services.Observability;

public sealed class SentryScopeService : ISentryScopeService
{
    public void SetUser(Guid userId)
    {
        if (!SentrySdk.IsEnabled) return;
        SentrySdk.ConfigureScope(scope =>
        {
            scope.User = new SentryUser { Id = userId.ToString() };
        });
    }

    public void ClearUser()
    {
        if (!SentrySdk.IsEnabled) return;
        SentrySdk.ConfigureScope(scope => scope.User = new SentryUser());
    }

    public void CaptureException(Exception ex, string? area = null)
    {
        if (!SentrySdk.IsEnabled) return;
        SentrySdk.CaptureException(ex, scope =>
        {
            if (!string.IsNullOrWhiteSpace(area))
                scope.SetTag("area", area);
        });
    }
}
