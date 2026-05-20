using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Services.Versioning;
using GdeOni.Mobile.Shared.Versioning;
using GdeOni.Mobile.Views.Auth;
using GdeOni.Mobile.Views.Profile;
using GdeOni.Mobile.Views.Tracked;

namespace GdeOni.Mobile;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;
    private readonly IAppVersionCheckService _versionCheck;
    private bool _initialNavigationDone;

    public AppShell(IAuthService authService, IAppVersionCheckService versionCheck)
    {
        InitializeComponent();
        _authService = authService;
        _versionCheck = versionCheck;

        // Auth flow.
        Routing.RegisterRoute("register", typeof(RegisterPage));

        // Tracked flow: deceased-search → deceased-preview → at-grave →
        // deceased-details + archive. Preview промежуточный шаг
        // между поиском и реальной подпиской (см. E17.1).
        Routing.RegisterRoute("deceased-search", typeof(DeceasedSearchPage));
        Routing.RegisterRoute("deceased-preview", typeof(DeceasedPreviewPage));
        Routing.RegisterRoute("at-grave", typeof(AtGravePage));
        Routing.RegisterRoute("deceased-details", typeof(DeceasedDetailsPage));
        Routing.RegisterRoute("archive", typeof(ArchivePage));
        Routing.RegisterRoute("memory-editor", typeof(MemoryEditorPage));
        Routing.RegisterRoute("burial-location-editor", typeof(BurialLocationEditorPage));

        // E21: поиск умерших в радиусе вокруг текущей точки пользователя.
        Routing.RegisterRoute("nearby-search", typeof(NearbySearchPage));

        // Profile flow: change-password — модальный push поверх профиля.
        Routing.RegisterRoute("change-password", typeof(ChangePasswordPage));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialNavigationDone)
            return;
        _initialNavigationDone = true;

        // E22. Сначала проверка версии — если клиент ниже MinSupportedVersion,
        // отправляем на blocking-update без возможности уйти. На сетевой
        // ошибке fail-open — пропускаем дальше.
        var versionResult = await _versionCheck.CheckAsync();
        if (versionResult.Outcome == VersionCheckOutcome.ForceUpdate)
        {
            var navParams = new Dictionary<string, object>
            {
                ["downloadUrl"] = versionResult.DownloadUrl ?? string.Empty,
                ["message"] = versionResult.ForceUpdateMessage ?? string.Empty,
            };
            await GoToAsync("//blocking-update", navParams);
            return;
        }

        // SoftUpdate пока не показываем — soft banner на главной приедет
        // отдельным коммитом (см. план E22).

        // Если в SecureStorage есть access-токен — пускаем сразу на главный
        // TabBar. RefreshTokenHandler сам разберётся, если токен протух.
        if (await _authService.HasSessionAsync())
            await GoToAsync("//main/tracked");
    }
}
