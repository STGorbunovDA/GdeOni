using GdeOni.API.Authorization;
using GdeOni.API.Extensions;
using GdeOni.API.Response;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaContent.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaContent.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D47. «Вахтёр» медиа. Плоский эндпоинт, который стримит файл (фото/могилы)
/// через сервер только авторизованному пользователю. Заменяет вечные
/// публичные ссылки MinIO: после закрытия анонимного доступа к бакетам
/// (MinioBootstrap, publicRead:false) прямой URL хранилища без входа отдаёт
/// отказ, а весь показ фото идёт через этот эндпоинт.
///
/// <para>
/// Маршрут плоский (<c>/api/media/{id}/content</c>, без deceasedId): сервер
/// сам кладёт готовый путь в поле url/photoUrl листингов и деталей, клиент
/// его только запрашивает своим авторизованным HTTP-клиентом (web axios с
/// Bearer, mobile — HttpClient с auth-хендлерами). Уровень —
/// BasicAuthenticated: достаточно входа, подписка не требуется (иначе не
/// грузились бы превью в basic-authenticated контекстах). Модерационную
/// видимость проверяет use case.
/// </para>
/// </summary>
[Route("api/media")]
[Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
[Tags("DeceasedRecords")]
public sealed class MediaContentController : ApiControllerBase
{
    /// <summary>
    /// Стримит файл медиа по его id. 200 — поток файла (inline),
    /// 404 — media нет / нет прав видеть неопубликованное.
    /// </summary>
    [HttpGet("{mediaId:guid}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContent(
        [FromRoute] Guid mediaId,
        [FromServices] IGetMediaContentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(new GetMediaContentQuery(mediaId), cancellationToken);

        if (result.IsFailure)
            return FromResult(result);

        var value = result.Value;

        // private: кэш только в браузере пользователя, не на общих прокси —
        // файл авторизационно-ограничен. max-age даёт повторным показам
        // (скролл списка, возврат на карточку) брать фото из HTTP-кэша, не
        // дёргая сервер каждый раз. immutable: storage_key уникален на файл,
        // содержимое по этому URL не меняется.
        Response.Headers.CacheControl = "private, max-age=86400, immutable";
        Response.Headers.ContentDisposition =
            $"inline; filename=\"{Uri.EscapeDataString(value.OriginalFileName)}\"";
        return File(value.File.Content, value.File.ContentType);
    }
}
