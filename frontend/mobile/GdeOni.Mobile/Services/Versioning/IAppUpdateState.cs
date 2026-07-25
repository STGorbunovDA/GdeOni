namespace GdeOni.Mobile.Services.Versioning;

/// <summary>
/// E22. Мост между проверкой версии (AppShell, один раз на старте) и мягким
/// баннером «доступна новая версия» на главной (TrackedListPage). Singleton:
/// проверка живёт в AppShell, а баннер рисует другой экран, созданный позже,
/// — состояние нужно где-то удержать между ними.
///
/// Force-update сюда НЕ попадает: он блокирует вход отдельной страницей ещё
/// до главной. Здесь только «мягкий» случай (current ≥ min, но &lt; latest).
/// </summary>
public interface IAppUpdateState
{
    /// <summary>Есть новая версия и баннер ещё не закрыт пользователем.</summary>
    bool IsSoftUpdateAvailable { get; }

    /// <summary>Ссылка на страницу скачивания (DownloadUrl с бэка).</summary>
    string? DownloadUrl { get; }

    /// <summary>Отметить, что доступно мягкое обновление (зовёт AppShell).</summary>
    void SetSoftUpdate(string? downloadUrl);

    /// <summary>
    /// Пользователь нажал «Позже» — прячем баннер до перезапуска приложения.
    /// Держим в памяти (singleton живёт сессию), намеренно не персистим: на
    /// новом старте снова напомним, пока не обновится.
    /// </summary>
    void Dismiss();
}

/// <inheritdoc />
public sealed class AppUpdateState : IAppUpdateState
{
    private bool _available;
    private bool _dismissed;

    public bool IsSoftUpdateAvailable => _available && !_dismissed;

    public string? DownloadUrl { get; private set; }

    public void SetSoftUpdate(string? downloadUrl)
    {
        _available = true;
        DownloadUrl = downloadUrl;
    }

    public void Dismiss() => _dismissed = true;
}
