using GdeOni.Application.Abstractions.Email;
using GdeOni.Application.Abstractions.Features;
using GdeOni.Application.Abstractions.Geo;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Routing;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Legal;
using GdeOni.Application.Subscriptions;
using GdeOni.Infrastructure.Data;
using GdeOni.Infrastructure.Email;
using GdeOni.Infrastructure.Features;
using GdeOni.Infrastructure.Geo;
using GdeOni.Infrastructure.Notifications;
using GdeOni.Infrastructure.Payments;
using GdeOni.Infrastructure.Persistence;
using GdeOni.Infrastructure.Persistence.Cleanup;
using GdeOni.Infrastructure.Persistence.Repositories;
using GdeOni.Infrastructure.Relatives;
using GdeOni.Infrastructure.Routing;
using GdeOni.Infrastructure.Security;
using GdeOni.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;

namespace GdeOni.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' не найдена.");

        services.AddDbContextPool<AppDbContext>(
            optionsAction: options =>
            {
                options.UseNpgsql(connectionString);
                options.UseSnakeCaseNamingConvention();

                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.LogTo(
                        Console.WriteLine,
                        new[] { DbLoggerCategory.Database.Command.Name },
                        LogLevel.Information);
                }
            },
            // Явный poolSize вместо EF-дефолта 1024 (D11.7.5). 128 хватит
            // для сценария "один кэш-инстанс на одновременный запрос";
            // если выше — можно поднять через перегрузку без изменения
            // кода вызывающих repos.
            poolSize: 128);

        services.AddScoped<IDeceasedRepository, DeceasedRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        // D23. История платежей.
        services.AddScoped<ISubscriptionPaymentRepository, SubscriptionPaymentRepository>();
        // D25. Обращения в службу поддержки (manual + auto-инциденты).
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        // Персональные настройки напоминаний о праздниках.
        services.AddScoped<IHolidayReminderRepository, HolidayReminderRepository>();
        // Ручные события пользователя.
        services.AddScoped<ICustomEventRepository, CustomEventRepository>();

        // D46. «Поделиться подборкой»: хранилище подборок + генератор
        // коротких кодов (без состояния → Singleton).
        services.AddScoped<IShareBundleRepository, ShareBundleRepository>();
        services.AddSingleton<IShareCodeFactory, ShareCodeFactory>();

        // F38. Read-model админской справки: только COUNT/SUM, без сущностей.
        services.AddScoped<IAdminStatsRepository, AdminStatsRepository>();

        // Функция «Родственники»: матчинг со-отслеживающих (Фаза 2) + диалоги
        // внутренней переписки (Фаза 3).
        services.AddScoped<IRelativesRepository, RelativesRepository>();
        services.AddScoped<IRelativeConversationRepository, RelativeConversationRepository>();
        services.AddScoped<IRelativeReportRepository, RelativeReportRepository>();

        // Внутрисайтовые уведомления (обращения/жалобы). Сервис доставки —
        // в Application слое (INotificationService).
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // D41. Обратное геокодирование (координаты → город) через Nominatim.
        // Ходим с сервера, а не с клиента: иначе IP юзера ушёл бы во внешний
        // сервис в ЕС, что противоречит нашей же политике (5.3).
        services.Configure<GeocodingOptions>(
            configuration.GetSection(GeocodingOptions.SectionName));
        services.AddHttpClient<IReverseGeocoder, NominatimReverseGeocoder>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<GeocodingOptions>>().Value;
            NominatimReverseGeocoder.ConfigureClient(http, opts);
        });
        // Прямое геокодирование (город → координаты) — тот же клиент/настройки.
        services.AddHttpClient<IForwardGeocoder, NominatimForwardGeocoder>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<GeocodingOptions>>().Value;
            NominatimReverseGeocoder.ConfigureClient(http, opts);
        });
        // PasswordHasher без состояния: BCrypt.Net не использует поля
        // экземпляра. Singleton экономит аллокацию на каждый запрос
        // (см. D11.7.2).
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<RefreshTokenFactory>();
        services.AddSingleton<IRefreshTokenFactory>(sp => sp.GetRequiredService<RefreshTokenFactory>());
        // D43. Тот же экземпляр под вторым именем — ссылке восстановления
        // пароля нужна ровно та же криптография (32 байта + SHA-256).
        services.AddSingleton<ISecureTokenFactory>(sp => sp.GetRequiredService<RefreshTokenFactory>());

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        // D46. «Поделиться подборкой» — срок жизни ссылки/QR.
        services.Configure<SharingOptions>(configuration.GetSection(SharingOptions.SectionName));

        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        services.Configure<BCryptOptions>(configuration.GetSection(BCryptOptions.SectionName));

        // D17. Feature flags читаются через IOptionsMonitor для
        // hot-reload без рестарта. Сервис без состояния → Singleton.
        services.Configure<FeatureFlagsOptions>(
            configuration.GetSection(FeatureFlagsOptions.SectionName));
        services.AddSingleton<IFeatureFlagService, OptionsFeatureFlagService>();

        // D16. Subscription opts + платёжный провайдер.
        services.Configure<SubscriptionOptions>(
            configuration.GetSection(SubscriptionOptions.SectionName));
        services.Configure<YooKassaOptions>(
            configuration.GetSection(YooKassaOptions.SectionName));

        // D19. Legal opts — версии и URL'ы Privacy/Terms. Биндим без
        // дополнительной обвязки: use case'ы читают IOptions<LegalOptions>.
        services.Configure<LegalOptions>(
            configuration.GetSection(LegalOptions.SectionName));

        // Выбор провайдера откладываем до resolution: на момент вызова
        // AddInfrastructure() configuration может быть ещё не полностью
        // собрана (в WebApplicationFactory ConfigureAppConfiguration
        // выполняется ПОСЛЕ AddInfrastructure). Поэтому регистрируем
        // обе реализации и решаем в factory через IOptions snapshot.
        services.AddSingleton<FakePaymentProvider>();
        services.AddHttpClient<YooKassaPaymentProvider>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<YooKassaOptions>>().Value;
            YooKassaPaymentProvider.ConfigureClient(http, opts);
        });

        // Scoped — иначе AddHttpClient<TClient> (Transient через
        // HttpClientFactory) попадёт в Singleton, что нарушает
        // правила и фиксирует один HttpClient на всё приложение
        // (важно для DNS-balancing).
        services.AddScoped<IPaymentProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<YooKassaOptions>>().Value;
            return opts.IsConfigured
                ? sp.GetRequiredService<YooKassaPaymentProvider>()
                : sp.GetRequiredService<FakePaymentProvider>();
        });

        services.AddSingleton<IMinioClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MinioOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.Endpoint))
                throw new InvalidOperationException("MinIO Endpoint не сконфигурирован.");
            if (string.IsNullOrWhiteSpace(options.AccessKey))
                throw new InvalidOperationException("MinIO AccessKey не сконфигурирован.");
            if (string.IsNullOrWhiteSpace(options.SecretKey))
                throw new InvalidOperationException("MinIO SecretKey не сконфигурирован.");

            // PublicBaseUrl опционален, но если задан — обязан быть
            // валидным абсолютным URL'ом. Без проверки typo вроде
            // "http//example.com" (нет двоеточия) приводил к тому, что
            // MinioFileStorage возвращал presigned URL с внутренним
            // адресом "minio:9000", который недоступен извне — фото
            // не отображались, ошибка находилась только в проде.
            if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl)
                && !Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException(
                    $"MinIO PublicBaseUrl невалиден: '{options.PublicBaseUrl}'. " +
                    "Ожидается абсолютный URL (например, https://cdn.example.com).");
            }

            var builder = new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey);

            if (options.UseSsl)
                builder = builder.WithSSL();

            return builder.Build();
        });

        services.AddSingleton<IFileStorage, MinioFileStorage>();

        // D33. Резолвер bucket'а для вложений в обращения. Отдельный
        // singleton без состояния — читает из MinioOptions.
        services.AddSingleton<ISupportAttachmentsBucketResolver, SupportAttachmentsBucketResolver>();

        // D8.2: deep-link провайдеры карт. Singleton — без состояния,
        // только форматируют URL без сетевых вызовов.
        services.AddSingleton<IRouteLinkProvider, YandexRouteLinkProvider>();
        services.AddSingleton<IRouteLinkProvider, GoogleMapsRouteLinkProvider>();
        services.AddSingleton<IRouteLinkProvider, TwoGisRouteLinkProvider>();

        services.AddHostedService<MinioOrphanCleanupService>();

        services.Configure<RefreshTokensCleanupOptions>(
            configuration.GetSection(RefreshTokensCleanupOptions.SectionName));
        services.AddHostedService<RefreshTokensCleanupService>();

        // D37. Email-канал + фоновая рассылка о годовщинах. Sender без
        // состояния → Singleton. Если SMTP не сконфигурирован (нет Host/
        // FromEmail) — подставляем no-op, чтобы приложение поднялось без
        // почтового сервера (dev / integration-тесты). Зеркалит выбор
        // FakePaymentProvider vs YooKassaPaymentProvider.
        services.Configure<EmailOptions>(
            configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<SmtpEmailSender>();
        services.AddSingleton<NoOpEmailSender>();
        services.AddSingleton<IEmailSender>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
            return opts.IsConfigured
                ? sp.GetRequiredService<SmtpEmailSender>()
                : sp.GetRequiredService<NoOpEmailSender>();
        });

        services.Configure<AnniversaryEmailOptions>(
            configuration.GetSection(AnniversaryEmailOptions.SectionName));
        services.AddHostedService<AnniversaryEmailService>();

        // Функция «Родственники» (Фаза 4). Ночной джоб ищет новых
        // родственников и заводит уведомления. Внешних зависимостей нет →
        // включён по умолчанию (см. RelativeDiscoveryOptions.Enabled).
        services.Configure<RelativeDiscoveryOptions>(
            configuration.GetSection(RelativeDiscoveryOptions.SectionName));
        services.AddHostedService<RelativeDiscoveryService>();

        return services;
    }

    public static Task SeedDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        return DbInitializer.SeedSuperAdminAsync(services, cancellationToken);
    }

    /// <summary>
    /// Fail-fast старта: бросает исключение если на БД не накатаны
    /// миграции из сборки. См. <see cref="DbInitializer.EnsureMigrationsAppliedAsync"/>.
    /// </summary>
    public static Task EnsureDatabaseMigrationsAppliedAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        return DbInitializer.EnsureMigrationsAppliedAsync(services, cancellationToken);
    }

    public static Task BootstrapStorageAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        return MinioBootstrap.EnsureBucketsAsync(services, cancellationToken);
    }
}