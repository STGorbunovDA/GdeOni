namespace GdeOni.Mobile.Services.Observability;

/// <summary>
/// E25. Тонкая обёртка над <c>SentrySdk.ConfigureScope</c> для управления
/// User-scope после Login/Logout. Тестируем через mock; интерфейс не
/// тянет Sentry в Application/ViewModels слои (избегаем прямого
/// SentrySdk там).
/// </summary>
public interface ISentryScopeService
{
    /// <summary>
    /// Привязывает crash-репорты к конкретному userId (no email — 152-ФЗ
    /// privacy-by-default). Вызывается после успешного Login.
    /// </summary>
    void SetUser(Guid userId);

    /// <summary>
    /// Сбрасывает userId со scope — после Logout. Также при
    /// принудительном logout от 401/SecurityStamp invalidation.
    /// </summary>
    void ClearUser();

    /// <summary>
    /// Захват исключения с дополнительным тегом area (название модуля /
    /// VM для удобной группировки в Sentry). Если DSN не настроен —
    /// SDK no-op.
    /// </summary>
    void CaptureException(Exception ex, string? area = null);
}
