using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Services.Notifications;
using GdeOni.Mobile.Services.Subscriptions;
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
    private readonly IPaywallChecker _paywallChecker;
    private readonly AnniversariesSyncService _anniversariesSync;
    private bool _initialNavigationDone;

    public AppShell(
        IAuthService authService,
        IAppVersionCheckService versionCheck,
        IPaywallChecker paywallChecker,
        AnniversariesSyncService anniversariesSync)
    {
        InitializeComponent();
        _authService = authService;
        _versionCheck = versionCheck;
        _paywallChecker = paywallChecker;
        _anniversariesSync = anniversariesSync;

        // Auth flow.
        Routing.RegisterRoute("register", typeof(RegisterPage));
        // D43. Восстановление пароля: запрос ссылки. Сама смена пароля
        // происходит на сайте — ссылка из письма открывается браузером.
        Routing.RegisterRoute(
            "forgot-password", typeof(GdeOni.Mobile.Views.Auth.ForgotPasswordPage));

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
        // E26. Редактирование карточки умершего (трекающий или админ).
        Routing.RegisterRoute("edit-deceased", typeof(EditDeceasedPage));
        // F17.9 mobile. История правок — только админам (бэк отдаст 403).
        Routing.RegisterRoute("edits-history", typeof(GdeOni.Mobile.Views.Admin.EditsHistoryPage));
        // F17.9 mobile. Админ-меню и подстраницы (доступ — кнопка из Profile).
        Routing.RegisterRoute("admin", typeof(GdeOni.Mobile.Views.Admin.AdminPage));
        Routing.RegisterRoute("all-edits", typeof(GdeOni.Mobile.Views.Admin.AllEditsHistoryPage));
        Routing.RegisterRoute("admin-users", typeof(GdeOni.Mobile.Views.Admin.AdminUsersPage));
        Routing.RegisterRoute("admin-user-details", typeof(GdeOni.Mobile.Views.Admin.AdminUserDetailsPage));
        Routing.RegisterRoute("admin-user-tracked", typeof(GdeOni.Mobile.Views.Admin.AdminUserTrackedPage));
        Routing.RegisterRoute("admin-payments", typeof(GdeOni.Mobile.Views.Admin.AdminPaymentsPage));
        Routing.RegisterRoute("admin-info", typeof(GdeOni.Mobile.Views.Admin.AdminInfoPage));
        // D25 mobile. Обращения — юзерские страницы и админ-страницы.
        Routing.RegisterRoute("support-new", typeof(GdeOni.Mobile.Views.Support.SupportNewPage));
        Routing.RegisterRoute("support-mine", typeof(GdeOni.Mobile.Views.Support.SupportMinePage));
        Routing.RegisterRoute("support-details", typeof(GdeOni.Mobile.Views.Support.SupportDetailsPage));
        Routing.RegisterRoute("admin-support", typeof(GdeOni.Mobile.Views.Admin.AdminSupportPage));
        Routing.RegisterRoute("admin-support-details", typeof(GdeOni.Mobile.Views.Admin.AdminSupportDetailsPage));
        // D27. Поиск умершего + admin-просмотр без отслеживания.
        Routing.RegisterRoute("admin-find-deceased", typeof(GdeOni.Mobile.Views.Admin.AdminFindDeceasedPage));
        Routing.RegisterRoute("admin-deceased-view", typeof(GdeOni.Mobile.Views.Admin.AdminDeceasedViewPage));
        // D27.1. Полноэкранный просмотр фото — открывается по тапу на
        // плитку фото в галерее. Доступно всем (юзеру и админу).
        Routing.RegisterRoute("photo-viewer", typeof(GdeOni.Mobile.Views.Common.FullScreenPhotoPage));

        // E21: поиск умерших в радиусе вокруг текущей точки пользователя.
        Routing.RegisterRoute("nearby-search", typeof(NearbySearchPage));

        // Profile flow: change-password — модальный push поверх профиля.
        Routing.RegisterRoute("change-password", typeof(ChangePasswordPage));

        // E22. Subscription flow — открывается из ProfilePage.
        Routing.RegisterRoute("subscription", typeof(SubscriptionPage));
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

        // Если в SecureStorage нет access-токена — остаёмся на login.
        if (!await _authService.HasSessionAsync())
            return;

        // E23 C.1. Восстанавливаем annivers-alarms в фоне. Запуск на старте
        // покрывает случай "юзер открыл приложение, но логиниться не пришлось
        // (токен ещё валиден) — sync после LoginViewModel сюда не дошёл".
        _ = Task.Run(() => _anniversariesSync.SyncAsync());

        // E22.6. После логина — проверяем нужен ли paywall.
        if (await _paywallChecker.ShouldShowPaywallAsync())
        {
            await GoToAsync("//subscription-required");
            return;
        }

        await GoToAsync("//main/tracked");
    }
}
