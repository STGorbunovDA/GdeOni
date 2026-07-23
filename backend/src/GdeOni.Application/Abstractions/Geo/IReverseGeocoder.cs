using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Geo;

/// <summary>
/// D41. Обратное геокодирование: координаты → адрес (страна / регион /
/// город).
///
/// Живёт на бэкенде, а не на клиенте, сознательно: прямой запрос из
/// браузера или мобилки отправил бы IP пользователя во внешний
/// геокодер (Nominatim — серверы в ЕС), а Политика конфиденциальности
/// (раздел 5.3) обещает, что трансграничной передачи персональных данных
/// нет. Через наш сервер наружу уходят только координаты могилы —
/// это не персональные данные пользователя.
///
/// Абстракция нужна, чтобы сменить провайдера (Nominatim → Яндекс.Геокодер
/// с ключом) без правки use case'ов и клиентов.
/// </summary>
public interface IReverseGeocoder
{
    /// <summary>
    /// Определяет адрес по координатам. Возвращает Failure, если геокодер
    /// выключен, недоступен или ничего не нашёл — это НЕ ошибка сценария,
    /// клиент просто оставит поля пустыми и заполнит их руками.
    /// </summary>
    Task<Result<ReverseGeocodeResult, Error>> Reverse(
        double latitude,
        double longitude,
        CancellationToken cancellationToken);
}

/// <summary>
/// Результат обратного геокодирования. Любое поле может быть null:
/// посреди леса не будет города, в море — вообще ничего.
/// </summary>
public sealed record ReverseGeocodeResult(
    string? Country,
    string? Region,
    string? City);
