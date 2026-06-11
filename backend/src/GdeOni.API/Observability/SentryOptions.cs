namespace GdeOni.API.Observability;

/// <summary>
/// D21. Настройки Sentry .NET для прод-наблюдения за необработанными
/// исключениями. Биндится из секции <c>Sentry</c> в appsettings.
///
/// Если <see cref="Dsn"/> пуст (default), Sentry не инициализируется —
/// удобно для локальной разработки и integration-тестов, чтобы не
/// заводить fake DSN.
/// </summary>
public sealed class SentryOptions
{
    public const string SectionName = "Sentry";

    /// <summary>
    /// DSN из проекта в sentry.io (или self-hosted Sentry). Без него
    /// Sentry не запускается, ошибки идут только в Serilog/Seq.
    /// </summary>
    public string? Dsn { get; set; }

    /// <summary>
    /// Environment tag в Sentry — "development" / "staging" / "production".
    /// </summary>
    public string Environment { get; set; } = "development";

    /// <summary>
    /// Версия бэка для группировки ошибок по релизам. Обычно
    /// заполняется через CI пайплайн на тег — например, "backend-v1.2.3".
    /// null = Sentry возьмёт assembly version.
    /// </summary>
    public string? Release { get; set; }

    /// <summary>
    /// Семплинг трейсов (производительность). 0.0 = ничего, 1.0 = всё.
    /// Для прода обычно 0.05–0.2 — баланс между видимостью и стоимостью.
    /// </summary>
    public double TracesSampleRate { get; set; }

    public bool Enabled => !string.IsNullOrWhiteSpace(Dsn);
}
