using GdeOni.API.Response;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.Model;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.HasPhotos.Model;
using GdeOni.Application.DeceasedRecords.Queries.HasPhotos.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер для управления карточками умерших.
/// </summary>
[Route("api/deceased-records")]
public sealed class DeceasedRecordsSupportiveController : ApiControllerBase
{
    /// <summary>
    /// Возвращает расстояние от переданных координат до места захоронения.
    /// </summary>
    [HttpGet("{id:guid}/distance")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetDistanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDistance(
        [FromRoute] Guid id,
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromServices] IGetDistanceUseCase getDistanceUseCase,
        CancellationToken cancellationToken)
    {
        var query = new GetDistanceQuery(id, latitude, longitude);
        var result = await getDistanceUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Возвращает возраст на момент смерти.
    /// </summary>
    [HttpGet("{id:guid}/age-at-death")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetAgeAtDeathResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetAgeAtDeathResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAgeAtDeath(
        [FromRoute] Guid id,
        [FromServices] IGetAgeAtDeathUseCase getAgeAtDeathUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getAgeAtDeathUseCase.Execute(new GetAgeAtDeathQuery(id), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Проверяет, есть ли у карточки фотографии.
    /// </summary>
    [HttpGet("{id:guid}/has-photos")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<HasPhotosResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HasPhotosResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HasPhotos(
        [FromRoute] Guid id,
        [FromServices] IHasPhotosUseCase hasPhotosUseCase,
        CancellationToken cancellationToken)
    {
        var query = new HasPhotosQuery(id);
        var result = await hasPhotosUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Проверяет, есть ли у карточки воспоминания.
    /// </summary>
    [HttpGet("{id:guid}/has-memories")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<HasMemoriesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HasMemoriesResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HasMemories(
        [FromRoute] Guid id,
        [FromServices] IHasMemoriesUseCase hasMemoriesUseCase,
        CancellationToken cancellationToken)
    {
        var query = new HasMemoriesQuery(id);
        var result = await hasMemoriesUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }
}