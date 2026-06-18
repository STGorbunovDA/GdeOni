using CSharpFunctionalExtensions;
using GdeOni.API.Extensions;
using GdeOni.API.Mappers;
using GdeOni.API.Models.DeceasedRecords;
using GdeOni.API.Response;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.Model;
using GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UploadMedia.Model;
using GdeOni.Application.DeceasedRecords.Commands.UploadMedia.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.DownloadMedia.Model;
using GdeOni.Application.DeceasedRecords.Queries.DownloadMedia.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaById.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaById.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaList.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaList.UseCase;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Управление медиафайлами умершего: фото умершего, фото могилы, документы.
/// Бинарные файлы хранятся в MinIO, метаданные — в PostgreSQL.
///
/// <para>
/// D26. Запись (POST/PATCH/DELETE) разрешена только админам — снимаем юр.
/// риск пользовательской выкладки чужих фото/документов. Чтение (GET)
/// остаётся доступным любому авторизованному. Per-action атрибут
/// <c>[Authorize(Roles=...)]</c> на write-эндпоинтах — контроллер же
/// сохраняет общий <c>[Authorize]</c> для GET'ов.
/// </para>
/// </summary>
[Route("api/deceased-records/{id:guid}/media")]
[Authorize]
[Tags("DeceasedRecords")]
public sealed class DeceasedMediaController : ApiControllerBase
{
    private const string AdminRoles = "SuperAdmin,Admin";

    /// <summary>
    /// Загружает медиафайл и сохраняет метаданные.
    /// MIME и размер валидируются по виду файла (фото 10 MB, PDF 25 MB).
    /// При ошибке БД файл удаляется из MinIO (best-effort).
    /// D26: доступно только администраторам.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = AdminRoles)]
    // Лимит = max(MaxPhotoSizeBytes, MaxDocumentSizeBytes) из FileValidator.
    // Раньше было хардкод 50 MB — шире чем валидатор; атакующий мог
    // прокинуть 49 MB и съесть память до отбоя на per-kind проверке.
    // Теперь сервер ASP.NET сам отрубает запросы > 25 MB на уровне TCP.
    [RequestSizeLimit(FileValidator.MaxDocumentSizeBytes)]
    [ProducesResponseType(typeof(ApiResponse<UploadMediaResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Upload(
        [FromRoute] Guid id,
        [FromForm] IFormFile file,
        [FromForm] MediaKind kind,
        [FromForm] string? description,
        [FromServices] IUploadMediaUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return FromResult(Result.Failure<UploadMediaResponse, Error>(Errors.Media.FileRequired()));

        await using var stream = file.OpenReadStream();

        var command = new UploadMediaCommand
        {
            DeceasedId = id,
            Kind = kind,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            Content = stream,
            Description = description
        };

        var result = await useCase.Execute(command, cancellationToken);
        return FromResult(
            result,
            r => r.ToCreatedResponse($"/api/deceased-records/{id}/media/{r.MediaId}"));
    }

    /// <summary>
    /// Возвращает список медиафайлов умершего с пагинацией и фильтрами.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<MediaListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetList(
        [FromRoute] Guid id,
        [FromQuery] GetMediaListRequest request,
        [FromServices] IGetMediaListUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request.ToQuery(id), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Возвращает метаданные одного медиафайла и URL для скачивания
    /// (public для фото, presigned для документов).
    /// </summary>
    [HttpGet("{mediaId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MediaDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromRoute] Guid mediaId,
        [FromServices] IGetMediaByIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            DeceasedRecordsMapping.ToGetMediaByIdQuery(id, mediaId),
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// F13 (web). Прокси-скачивание файла через сервер: бэк сам
    /// тянет поток из MinIO и отдаёт клиенту. Нужен потому что
    /// presigned URL'ы в dev-конфиге используют host
    /// <c>10.0.2.2:9000</c> (Android-эмулятор) — браузер на Windows
    /// этот host не разрешает. На production даёт дополнительные
    /// возможности: скрыть внутренний хост MinIO и логировать
    /// доступ к файлам.
    ///
    /// Mobile эту прокси НЕ ИСПОЛЬЗУЕТ — у него Launcher.OpenAsync
    /// открывает presigned URL напрямую (10.0.2.2 на эмуляторе
    /// разрешается).
    /// </summary>
    [HttpGet("{mediaId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        [FromRoute] Guid id,
        [FromRoute] Guid mediaId,
        [FromServices] IDownloadMediaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            new DownloadMediaQuery(id, mediaId),
            cancellationToken);

        if (result.IsFailure)
            return FromResult(result);

        var value = result.Value;
        // FileStreamResult сам задиспозит поток после отправки клиенту.
        // Content-Disposition=inline — браузер откроет PDF в viewer'е,
        // не предложит "сохранить как".
        Response.Headers.ContentDisposition =
            $"inline; filename=\"{Uri.EscapeDataString(value.OriginalFileName)}\"";
        return File(value.File.Content, value.File.ContentType);
    }

    /// <summary>
    /// Удаляет медиафайл. D26: доступно только администраторам.
    /// Сначала удаляются метаданные из БД, затем файл из MinIO (best-effort).
    /// </summary>
    [HttpDelete("{mediaId:guid}")]
    [Authorize(Roles = AdminRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        [FromRoute] Guid mediaId,
        [FromServices] IDeleteMediaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            DeceasedRecordsMapping.ToDeleteMediaCommand(id, mediaId),
            cancellationToken);

        return FromUnitResult(result);
    }

    /// <summary>
    /// Обновляет описание медиафайла. D26: доступно только администраторам.
    /// Пустое тело очищает описание.
    /// </summary>
    [HttpPatch("{mediaId:guid}/description")]
    [Authorize(Roles = AdminRoles)]
    [ProducesResponseType(typeof(ApiResponse<UpdateMediaDescriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDescription(
        [FromRoute] Guid id,
        [FromRoute] Guid mediaId,
        [FromBody] UpdateMediaDescriptionRequest request,
        [FromServices] IUpdateMediaDescriptionUseCase useCase,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, mediaId);
        var result = await useCase.Execute(command, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Назначает фото главным фото умершего.
    /// Только для MediaKind.DeceasedPhoto. D26: доступно только администраторам.
    /// </summary>
    [HttpPatch("{mediaId:guid}/main-photo")]
    [Authorize(Roles = AdminRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetMainPhoto(
        [FromRoute] Guid id,
        [FromRoute] Guid mediaId,
        [FromServices] ISetMainMediaPhotoUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(
            DeceasedRecordsMapping.ToSetMainMediaPhotoCommand(id, mediaId),
            cancellationToken);

        return FromUnitResult(result);
    }
}
