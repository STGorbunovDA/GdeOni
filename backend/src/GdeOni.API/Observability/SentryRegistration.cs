namespace GdeOni.API.Observability;

/// <summary>
/// D21. Регистрация Sentry в WebApplicationBuilder. Вынесено в отдельный
/// extension, чтобы Program.cs не разрастался и потому что Sentry
/// поднимается на уровне <c>WebHost</c>, а не <c>Services</c>.
/// </summary>
public static class SentryRegistration
{
    public static WebApplicationBuilder AddCustomSentry(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(SentryOptions.SectionName);
        builder.Services.Configure<SentryOptions>(section);

        var options = section.Get<SentryOptions>() ?? new SentryOptions();
        if (!options.Enabled)
        {
            // Без DSN не инициализируем Sentry. Это критично для:
            //  - local-dev: разработчик не должен заводить fake DSN;
            //  - integration-тестов: WebApplicationFactory не должен
            //    стучаться в реальный Sentry на каждом прогоне.
            return builder;
        }

        builder.WebHost.UseSentry(o =>
        {
            o.Dsn = options.Dsn;
            o.Environment = options.Environment;
            if (!string.IsNullOrWhiteSpace(options.Release))
                o.Release = options.Release;
            o.TracesSampleRate = options.TracesSampleRate;

            // SendDefaultPii=false → не отправляем email/IP/cookies
            // юзера в Sentry. Из claim'ов остаётся только sub (userId).
            // Это согласовано с 152-ФЗ и Privacy Policy (D19).
            o.SendDefaultPii = false;
            o.AttachStacktrace = true;

            // Sentry-логгер захватывает Serilog-события начиная с Error+;
            // Warning из request-логов не засоряет inbox.
            o.MinimumEventLevel = LogLevel.Error;
        });

        return builder;
    }
}
