namespace GdeOni.Mobile.Services.Versioning;

/// <summary>
/// E22. Мост между проверкой версии (AppShell, один раз на старте) и диалогом
/// обновления «доступна новая версия» на главной (TrackedListPage). Singleton:
/// проверка живёт в AppShell, а диалог показывает другой экран, созданный позже,
/// — состояние нужно где-то удержать между ними.
///
/// Force-update сюда НЕ попадает: он блокирует вход отдельной страницей ещё
/// до главной. Здесь только «мягкий» случай (current ≥ min, но &lt; latest).
/// </summary>
public interface IAppUpdateState
{
    /// <summary>
    /// Есть новая версия и диалог обновления ещё не показывали в этой сессии.
    /// </summary>
    bool IsSoftUpdateAvailable { get; }

    /// <summary>Ссылка на страницу скачивания (DownloadUrl с бэка).</summary>
    string? DownloadUrl { get; }

    /// <summary>Отметить, что доступно мягкое обновление (зовёт AppShell).</summary>
    void SetSoftUpdate(string? downloadUrl);

    /// <summary>
    /// Пометить, что диалог обновления уже показали — чтобы он всплывал один
    /// раз за запуск, а не при каждом заходе на главную. Держим в памяти
    /// (singleton живёт сессию), намеренно не персистим: на новом старте снова
    /// напомним, пока пользователь не обновится.
    /// </summary>
    void MarkPrompted();
}

/// <inheritdoc />
public sealed class AppUpdateState : IAppUpdateState
{
    private bool _available;
    private bool _prompted;

    public bool IsSoftUpdateAvailable => _available && !_prompted;

    public string? DownloadUrl { get; private set; }

    public void SetSoftUpdate(string? downloadUrl)
    {
        _available = true;
        DownloadUrl = downloadUrl;
    }

    public void MarkPrompted() => _prompted = true;
}
