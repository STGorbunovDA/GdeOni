using CommunityToolkit.Maui;
using GdeOni.Mobile.Services;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Services.Geolocation;
using GdeOni.Mobile.Services.Network;
using GdeOni.Mobile.Services.Notifications;
using GdeOni.Mobile.Services.Observability;
using GdeOni.Mobile.Services.Routing;
using GdeOni.Mobile.Services.Storage;
using GdeOni.Mobile.Services.Subscriptions;
using GdeOni.Mobile.Services.Versioning;
using GdeOni.Mobile.Shared.Notifications;
using GdeOni.Mobile.ViewModels;
using GdeOni.Mobile.Views.Auth;
using GdeOni.Mobile.Views.Profile;
using GdeOni.Mobile.Views.Route;
using GdeOni.Mobile.Views.Subscriptions;
using GdeOni.Mobile.Views.Tracked;
using GdeOni.Mobile.Views.Updates;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Polly;
using Polly.Extensions.Http;
using Refit;

namespace GdeOni.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        // E25. Подключаем Sentry до UseMauiApp, чтобы перехватить crash'и
        // в инициализации тоже. AppConfig читается синхронно один раз для
        // вытаскивания DSN; если DSN null/empty — UseSentry no-op (SDK
        // ничего не отправляет, никаких накладных).
        var appConfig = AppConfigLoader.Load();
        if (!string.IsNullOrWhiteSpace(appConfig.Sentry?.Dsn))
        {
            builder.UseSentry(options =>
            {
                options.Dsn = appConfig.Sentry.Dsn;
                options.Environment = appConfig.Sentry.Environment ?? appConfig.Environment;
                options.Release = appConfig.Sentry.Release;
                options.TracesSampleRate = appConfig.Sentry.TracesSampleRate;
                // 152-ФЗ: не отправляем PII по умолчанию. User-Id ставим
                // вручную после Login (см. SentryScopeService).
                options.SendDefaultPii = false;
                options.AttachStacktrace = true;
                options.MinimumBreadcrumbLevel = LogLevel.Information;
                options.MinimumEventLevel = LogLevel.Error;
            });
        }

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            // UseMauiMaps() убран: нативная Google-карта в приложении не
            // используется (выбор точки — WebView + Leaflet, см.
            // Controls/MapPickerView), а Google Maps SDK без meta-data
            // com.google.android.geo.API_KEY в манифесте роняет процесс:
            //   IllegalStateException: API key not found → SIGSEGV.
            // Если когда-нибудь понадобится нативная карта — вернуть вызов
            // И добавить API-ключ в AndroidManifest.
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                // Шрифты дизайна Medilab (зеркало web). Poppins сюда НЕ берём:
                // в нём нет кириллицы, а в MAUI нет unicode-range-фолбэка как
                // в CSS — русские заголовки просто не отрисовались бы. Их роль
                // играет Montserrat (геометрический, с кириллицей).
                fonts.AddFont("Roboto-Regular.ttf", "Roboto");
                fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
                fonts.AddFont("Montserrat-SemiBold.ttf", "Montserrat");
                fonts.AddFont("Montserrat-Bold.ttf", "MontserratBold");
                fonts.AddFont("Raleway-SemiBold.ttf", "Raleway");
            });

        // AppConfig — singleton: уже загрузили выше для Sentry. Реюзаем
        // тот же инстанс, не читаем asset повторно.
        builder.Services.AddSingleton(appConfig);

        RegisterStorage(builder.Services);
        RegisterHttpStack(builder.Services);
        RegisterPagesAndViewModels(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void RegisterStorage(IServiceCollection services)
    {
        services.AddSingleton<ITokenStore, SecureTokenStore>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IGeolocationService, GeolocationService>();
        services.AddSingleton<INetworkInfoService, NetworkInfoService>();
        services.AddSingleton<IExternalMapsService, ExternalMapsService>();
        // D36. Сервис построения публичных URL медиа из bucket+storageKey.
        // Кеширует MediaBaseUrl из /api/app/features на сессию.
        services.AddSingleton<Services.Media.IPublicHostsService,
            Services.Media.PublicHostsService>();
        // E22. Version-gate сервис — Singleton, чтобы при future
        // расширении (кеш на сессию) состояние переживало переходы между
        // страницами.
        services.AddSingleton<IAppVersionCheckService, AppVersionCheckService>();
        // E25. Sentry scope service — управление SentryUser после
        // Login/Logout. Stateless, безопасен как Singleton.
        services.AddSingleton<ISentryScopeService, SentryScopeService>();
        // E23. Локальные уведомления о годовщинах. На Android —
        // AlarmManager-реализация; на остальных платформах NoOp-stub,
        // чтобы DI-контракт всегда резолвился (ViewModel'и не пишут
        // if-ы про платформу).
#if ANDROID
        services.AddSingleton<ILocalNotificationScheduler,
            Platforms.Android.Notifications.AndroidAlarmScheduler>();
#else
        services.AddSingleton<ILocalNotificationScheduler,
            Services.Notifications.NoOpLocalNotificationScheduler>();
#endif
        // E22.6. Paywall-checker (на старте) и DelegatingHandler (в
        // середине сессии).
        services.AddSingleton<IPaywallChecker, PaywallChecker>();
    }

    private static void RegisterHttpStack(IServiceCollection services)
    {
        services.AddTransient<AuthTokenHandler>();
        services.AddTransient<RefreshTokenHandler>();
        // E22.6. Перехват 403 subscription.required — добавляется только
        // в gated клиенты ниже (не whitelist'овые auth/users/app/subscriptions).
        services.AddTransient<SubscriptionGateHandler>();
        services.AddSingleton<IRefreshHttpClientProvider, RefreshHttpClientProvider>();

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
        };

        // Изолированный HttpClient под /auth/refresh — без AuthTokenHandler/RefreshTokenHandler.
        services.AddHttpClient(RefreshHttpClientProvider.ClientName, (sp, client) =>
        {
            var config = sp.GetRequiredService<AppConfig>();
            client.BaseAddress = new Uri(config.Api.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
        });

        services.AddRefitClient<IAuthApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddPolicyHandler(BuildRetryPolicy());

        services.AddRefitClient<IUsersApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddPolicyHandler(BuildRetryPolicy());

        services.AddRefitClient<IDeceasedRecordsApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddHttpMessageHandler<SubscriptionGateHandler>()
            .AddPolicyHandler(BuildRetryPolicy());

        services.AddRefitClient<IAdminApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddPolicyHandler(BuildRetryPolicy());

        services.AddRefitClient<ITrackedDeceasedApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddHttpMessageHandler<SubscriptionGateHandler>()
            .AddPolicyHandler(BuildRetryPolicy());

        services.AddRefitClient<IDeceasedMediaApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                // Загрузка фото может занять больше времени из-за multipart upload.
                c.Timeout = TimeSpan.FromSeconds(Math.Max(60, config.Api.TimeoutSeconds));
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddHttpMessageHandler<SubscriptionGateHandler>();
            // Polly retry на multipart не вешаем — повторная отправка большого
            // файла на сетевые ошибки опаснее, чем сообщить юзеру и дать
            // нажать "Загрузить" ещё раз.

        services.AddRefitClient<IDeceasedMemoriesApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddHttpMessageHandler<SubscriptionGateHandler>()
            .AddPolicyHandler(BuildRetryPolicy());

        // E22. IAppApi: /api/app/version (AllowAnonymous, дёргаем на
        // старте до логина) + /api/app/features (Authorize, после логина).
        // AuthTokenHandler пропускает запрос без Authorization если токена
        // нет — для GetVersion это ожидаемо.
        services.AddRefitClient<IAppApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddPolicyHandler(BuildRetryPolicy());

        // E22. ISubscriptionsApi: get/create-payment/cancel — все Authorize
        // и в whitelist (BasicAuthenticated), доступны даже без активной
        // подписки.
        services.AddRefitClient<ISubscriptionsApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddPolicyHandler(BuildRetryPolicy());

        // События: справочник праздников. Gated как feature-клиент —
        // 403 subscription.required уводит на paywall (как и tracked).
        services.AddRefitClient<IEventsApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddHttpMessageHandler<SubscriptionGateHandler>()
            .AddPolicyHandler(BuildRetryPolicy());

        // D41. IGeoApi: координаты → город. Не гейтим подпиской: экраны,
        // где он нужен (добавление умершего, правка координат), и так за
        // paywall'ом, а 403 здесь испортил бы автоподсказку адреса.
        services.AddRefitClient<IGeoApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddPolicyHandler(BuildRetryPolicy());

        // D25. ISupportApi: обращения в поддержку. Доступно любому
        // authenticated юзеру — не нужно гейтить подпиской, иначе юзер
        // с истёкшей подпиской не сможет пожаловаться "не работает оплата".
        services.AddRefitClient<ISupportApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<AppConfig>();
                c.BaseAddress = new Uri(config.Api.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RefreshTokenHandler>()
            .AddPolicyHandler(BuildRetryPolicy());
    }

    /// <summary>
    /// 2 повторных попытки на network/5xx с exponential backoff (1s, 2s).
    /// 401/403/4xx не ретраим — это бизнес-ошибки.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                2,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)));

    private static void RegisterPagesAndViewModels(IServiceCollection services)
    {
        services.AddSingleton<AppShell>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        // D43. Восстановление пароля.
        services.AddTransient<ForgotPasswordViewModel>();
        services.AddTransient<TrackedListViewModel>();
        services.AddTransient<EventsViewModel>();
        services.AddTransient<RouteViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<AtGraveViewModel>();
        services.AddTransient<DeceasedDetailsViewModel>();
        services.AddTransient<ArchiveViewModel>();
        services.AddTransient<MemoryEditorViewModel>();
        services.AddTransient<DeceasedSearchViewModel>();
        services.AddTransient<DeceasedPreviewViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<BurialLocationEditorViewModel>();
        services.AddTransient<EditDeceasedViewModel>();
        services.AddTransient<EditsHistoryViewModel>();
        services.AddTransient<AdminViewModel>();
        services.AddTransient<AllEditsHistoryViewModel>();
        services.AddTransient<AdminUsersViewModel>();
        services.AddTransient<AdminUserDetailsViewModel>();
        services.AddTransient<AdminUserTrackedViewModel>();
        services.AddTransient<AdminPaymentsViewModel>();
        services.AddSingleton<AnniversariesSyncService>();
        services.AddTransient<NearbySearchViewModel>();
        services.AddTransient<BlockingUpdateViewModel>();
        services.AddTransient<SubscriptionViewModel>();
        services.AddTransient<SubscriptionRequiredViewModel>();

        // D25 mobile. Обращения в поддержку.
        services.AddTransient<SupportNewViewModel>();
        services.AddTransient<SupportMineViewModel>();
        services.AddTransient<SupportDetailsViewModel>();
        services.AddTransient<AdminSupportViewModel>();
        services.AddTransient<AdminSupportDetailsViewModel>();

        // D27 mobile. Админ-поиск умершего и admin-просмотр без отслеживания.
        services.AddTransient<AdminFindDeceasedViewModel>();
        services.AddTransient<AdminDeceasedViewViewModel>();

        services.AddTransient<LoginPage>();
        services.AddTransient<RegisterPage>();
        services.AddTransient<GdeOni.Mobile.Views.Auth.ForgotPasswordPage>();
        services.AddTransient<TrackedListPage>();
        services.AddTransient<GdeOni.Mobile.Views.Events.EventsPage>();
        services.AddTransient<RoutePage>();
        services.AddTransient<ProfilePage>();
        services.AddTransient<AtGravePage>();
        services.AddTransient<DeceasedDetailsPage>();
        services.AddTransient<ArchivePage>();
        services.AddTransient<MemoryEditorPage>();
        services.AddTransient<DeceasedSearchPage>();
        services.AddTransient<DeceasedPreviewPage>();
        services.AddTransient<ChangePasswordPage>();
        services.AddTransient<BurialLocationEditorPage>();
        services.AddTransient<EditDeceasedPage>();
        services.AddTransient<GdeOni.Mobile.Views.Admin.EditsHistoryPage>();
        services.AddTransient<GdeOni.Mobile.Views.Admin.AdminPage>();
        services.AddTransient<GdeOni.Mobile.Views.Admin.AllEditsHistoryPage>();
        services.AddTransient<GdeOni.Mobile.Views.Admin.AdminUsersPage>();
        services.AddTransient<GdeOni.Mobile.Views.Admin.AdminUserDetailsPage>();
        services.AddTransient<GdeOni.Mobile.Views.Admin.AdminUserTrackedPage>();
        services.AddTransient<GdeOni.Mobile.Views.Admin.AdminPaymentsPage>();
        services.AddTransient<NearbySearchPage>();
        services.AddTransient<BlockingUpdatePage>();
        services.AddTransient<SubscriptionPage>();
        services.AddTransient<SubscriptionRequiredPage>();

        // D25 mobile. Pages обращений.
        services.AddTransient<GdeOni.Mobile.Views.Support.SupportNewPage>();
        services.AddTransient<GdeOni.Mobile.Views.Support.SupportMinePage>();
        services.AddTransient<GdeOni.Mobile.Views.Support.SupportDetailsPage>();
        services.AddTransient<GdeOni.Mobile.Views.Admin.AdminSupportPage>();
        services.AddTransient<GdeOni.Mobile.Views.Admin.AdminSupportDetailsPage>();

        // D27 mobile. Pages админ-поиска.
        services.AddTransient<GdeOni.Mobile.Views.Admin.AdminFindDeceasedPage>();
        services.AddTransient<GdeOni.Mobile.Views.Admin.AdminDeceasedViewPage>();

        // D27.1. Полноэкранный просмотр фото.
        services.AddTransient<GdeOni.Mobile.Views.Common.FullScreenPhotoPage>();
    }
}
