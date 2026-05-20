using CommunityToolkit.Maui;
using GdeOni.Mobile.Services;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Services.Geolocation;
using GdeOni.Mobile.Services.Network;
using GdeOni.Mobile.Services.Routing;
using GdeOni.Mobile.Services.Storage;
using GdeOni.Mobile.Services.Versioning;
using GdeOni.Mobile.ViewModels;
using GdeOni.Mobile.Views.Auth;
using GdeOni.Mobile.Views.Profile;
using GdeOni.Mobile.Views.Route;
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
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // AppConfig — lazy singleton: реально читается при первом обращении
        // (например, при первой настройке HttpClient'а в фоне). Так старт
        // приложения не блокируется чтением asset'а на main-потоке.
        builder.Services.AddSingleton(_ => AppConfigLoader.Load());

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
        // E22. Version-gate сервис — Singleton, чтобы при future
        // расширении (кеш на сессию) состояние переживало переходы между
        // страницами.
        services.AddSingleton<IAppVersionCheckService, AppVersionCheckService>();
    }

    private static void RegisterHttpStack(IServiceCollection services)
    {
        services.AddTransient<AuthTokenHandler>();
        services.AddTransient<RefreshTokenHandler>();
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
            .AddHttpMessageHandler<RefreshTokenHandler>();
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
        services.AddTransient<TrackedListViewModel>();
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
        services.AddTransient<NearbySearchViewModel>();
        services.AddTransient<BlockingUpdateViewModel>();
        services.AddTransient<SubscriptionViewModel>();

        services.AddTransient<LoginPage>();
        services.AddTransient<RegisterPage>();
        services.AddTransient<TrackedListPage>();
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
        services.AddTransient<NearbySearchPage>();
        services.AddTransient<BlockingUpdatePage>();
        services.AddTransient<SubscriptionPage>();
    }
}
