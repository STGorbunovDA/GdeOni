using GdeOni.API.Authorization;
using GdeOni.API.Mappers;
using GdeOni.API.Models.Sharing;
using GdeOni.API.Response;
using GdeOni.Application.Sharing.Commands.CreateShareBundle.Model;
using GdeOni.Application.Sharing.Commands.CreateShareBundle.UseCase;
using GdeOni.Application.Sharing.Commands.ImportShareBundle.Model;
using GdeOni.Application.Sharing.Commands.ImportShareBundle.UseCase;
using GdeOni.Application.Sharing.Queries.GetShareBundle.Model;
using GdeOni.Application.Sharing.Queries.GetShareBundle.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D46. «Поделиться подборкой карточек». Отправитель формирует подборку из
/// своих отслеживаемых карточек → получает короткий код (ссылка/QR).
/// Получатель открывает <c>/s/{code}</c>, входит и добавляет карточки себе
/// в отслеживание.
///
/// Публичной страницы нет: раскрытие и импорт требуют входа. Импорт —
/// под подпиской (точка конверсии), просмотр подборки — только вход.
/// </summary>
[ApiController]
[Tags("Sharing")]
[Route("api/share-bundles")]
public sealed class ShareBundlesController : ApiControllerBase
{
    /// <summary>
    /// Создаёт подборку из выбранных карточек и возвращает короткий код +
    /// срок действия. Полную ссылку/QR клиент строит от своего origin.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
    [ProducesResponseType(typeof(ApiResponse<CreateShareBundleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateShareBundleRequest request,
        [FromServices] ICreateShareBundleUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request.ToCommand(), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Раскрывает подборку по коду: строки карточек (ФИО/даты/место) для
    /// экрана получателя. 404 — код неизвестен или ссылка истекла.
    /// </summary>
    [HttpGet("{code}")]
    [Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
    [ProducesResponseType(typeof(ApiResponse<GetShareBundleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [FromRoute] string code,
        [FromServices] IGetShareBundleUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(new GetShareBundleQuery(code), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Импортирует подборку в отслеживание текущего пользователя (кнопка
    /// «Добавить»). Под подпиской — новый юзер на триале добавляет сразу,
    /// истёкший триал получит 403 subscription.required (paywall).
    /// </summary>
    [HttpPost("{code}/import")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ImportShareBundleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Import(
        [FromRoute] string code,
        [FromServices] IImportShareBundleUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(new ImportShareBundleCommand(code), cancellationToken);
        return FromResult(result);
    }
}
