using System.ComponentModel;
using System.Runtime.CompilerServices;
using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Services.Notifications;
using GdeOni.Mobile.Services.Subscriptions;
using GdeOni.Mobile.Services.Versioning;
using GdeOni.Mobile.Shared.Versioning;
using GdeOni.Mobile.Views.Auth;
using GdeOni.Mobile.Views.Profile;
using GdeOni.Mobile.Views.Tracked;

namespace GdeOni.Mobile;

public partial class AppShell : Shell, INotifyPropertyChanged
{
    private readonly IAuthService _authService;
    private readonly IAppVersionCheckService _versionCheck;
    private readonly IPaywallChecker _paywallChecker;
    private readonly AnniversariesSyncService _anniversariesSync;
    private bool _initialNavigationDone;
    private bool _isCurrentUserAdmin;

    /// <summary>
    /// F17.9 mobile. Биндится на IsVisible вкладки "Админка" в AppShell.xaml.
    /// Заполняется в OnAppearing после HasSessionAsync через
    /// authService.GetCurrentUserAsync(). На logout сбрасывается в false
    /// внешним кодом (ProfileViewModel после Logout зовёт ResetAdminFlag).
    /// </summary>
    public bool IsCurrentUserAdmin
    {
        get => _isCurrentUserAdmin;
        set
        {
            if (_isCurrentUserAdmin == value) return;
            _isCurrentUserAdmin = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCurrentUserAdmin)));
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

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
        // Глобальная админ-вкладка → подстраницы.
        Routing.RegisterRoute("all-edits", typeof(GdeOni.Mobile.Views.Admin.AllEditsHistoryPage));
        Routing.RegisterRoute("admin-users", typeof(GdeOni.Mobile.Views.Admin.AdminUsersPage));
        Routing.RegisterRoute("admin-user-details", typeof(GdeOni.Mobile.Views.Admin.AdminUserDetailsPage));
        Routing.RegisterRoute("admin-payments", typeof(GdeOni.Mobile.Views.Admin.AdminPaymentsPage));

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

        // F17.9 mobile. Подтягиваем роль ДО навигации в TabBar, чтобы
        // вкладка "Админка" появилась/спряталась сразу.
        await RefreshCurrentUserRoleAsync();

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

    /// <summary>
    /// Дёрнуть бэк, обновить флаг IsCurrentUserAdmin. Доступно публично —
    /// чтобы LoginViewModel после успешного логина мог сразу обновить TabBar
    /// без полного re-init AppShell.
    /// </summary>
    public async Task RefreshCurrentUserRoleAsync()
    {
        try
        {
            var me = await _authService.GetCurrentUserAsync();
            IsCurrentUserAdmin = me is not null && (
                string.Equals(me.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(me.Role, "Admin", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            // Сетевая ошибка / 401 — не валим UI, просто скрываем админку.
            IsCurrentUserAdmin = false;
        }
    }

    /// <summary>Сброс при logout, чтобы вкладка скрылась мгновенно.</summary>
    public void ResetAdminFlag() => IsCurrentUserAdmin = false;
}
