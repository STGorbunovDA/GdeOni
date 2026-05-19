using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Views.Auth;
using GdeOni.Mobile.Views.Profile;
using GdeOni.Mobile.Views.Tracked;

namespace GdeOni.Mobile;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;
    private bool _initialNavigationDone;

    public AppShell(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;

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

        // Если в SecureStorage есть access-токен — пускаем сразу на главный
        // TabBar. RefreshTokenHandler сам разберётся, если токен протух.
        if (await _authService.HasSessionAsync())
            await GoToAsync("//main/tracked");
    }
}
