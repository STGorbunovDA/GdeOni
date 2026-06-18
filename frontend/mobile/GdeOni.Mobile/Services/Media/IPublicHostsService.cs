namespace GdeOni.Mobile.Services.Media;

/// <summary>
/// D36. Сервис построения публичных URL медиа-файлов (фото).
///
/// Бэк отдаёт <c>MainPhotoBucket</c>+<c>MainPhotoStorageKey</c> в каждом
/// listing/details DTO и <c>MediaBaseUrl</c> в <c>/api/app/features</c>.
/// Клиент сам собирает URL — это снимает проблему «один URL для всех
/// клиентов», когда mobile-эмулятор и web имеют разные хост-маппинги
/// (10.0.2.2:9000 vs localhost:9000).
///
/// Кеш на сессию: <c>MediaBaseUrl</c> не меняется между релизами; при
/// рестарте процесса будет перезапрошен. Если бэк ещё старый (нет поля
/// MediaBaseUrl) или features-запрос упал — используем DEBUG-дефолт
/// <c>http://10.0.2.2:9000</c>.
/// </summary>
public interface IPublicHostsService
{
    /// <summary>
    /// Собирает абсолютный URL фото из bucket+storageKey. Возвращает
    /// null, если оба параметра пустые или mediaBaseUrl не доступен
    /// (нет связи, бэк не ответил, дефолт тоже не задан).
    /// </summary>
    Task<string?> BuildMediaUrlAsync(
        string? bucket,
        string? storageKey,
        CancellationToken cancellationToken = default);
}
