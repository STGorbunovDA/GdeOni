using GdeOni.API.Authorization;
using GdeOni.API.Response;
using GdeOni.Application.Geo.Queries.ForwardGeocode.Model;
using GdeOni.Application.Geo.Queries.ForwardGeocode.UseCase;
using GdeOni.Application.Geo.Queries.ReverseGeocode.Model;
using GdeOni.Application.Geo.Queries.ReverseGeocode.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D41. Геосервисы. Пока один: обратное геокодирование для автозаполнения
/// адреса при добавлении карточки и правке координат.
///
/// Ходим во внешний геокодер С СЕРВЕРА, а не из браузера/мобилки: прямой
/// запрос с клиента отправил бы IP пользователя в Nominatim (ЕС), а
/// Политика конфиденциальности (5.3) обещает отсутствие трансграничной
/// передачи персональных данных. Наружу уходят только координаты могилы.
/// </summary>
[ApiController]
[Tags("Geo")]
[Route("api/geo")]
public sealed class GeoController : ApiControllerBase
{
    /// <summary>
    /// Определяет страну / регион / город по координатам.
    ///
    /// 404 (geo.address.not_found) и 500 (geo.geocoding.unavailable) —
    /// штатные исходы: клиент просто не заполняет поля, юзер вписывает
    /// город руками. Сценарий добавления карточки от этого не ломается.
    /// </summary>
    [HttpGet("reverse")]
    [Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
    [ProducesResponseType(typeof(ApiResponse<ReverseGeocodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reverse(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromServices] IReverseGeocodeUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new ReverseGeocodeQuery(latitude, longitude),
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Ищет координаты по тексту адреса (город / кладбище). Форма «добавить
    /// умершего» подставляет по нему точку на карте, пока у пользователя ещё
    /// нет координат.
    ///
    /// 404 (geo.address.not_found) и 500 (geo.geocoding.unavailable) —
    /// штатные исходы: пользователь поставит точку на карте сам.
    /// </summary>
    [HttpGet("search")]
    [Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
    [ProducesResponseType(typeof(ApiResponse<ForwardGeocodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromServices] IForwardGeocodeUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new ForwardGeocodeQuery(query),
            cancellationToken);

        return FromResult(result);
    }
}
