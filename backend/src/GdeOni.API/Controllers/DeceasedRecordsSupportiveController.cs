using GdeOni.API.Mappers;
using GdeOni.API.Response;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.Model;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер для управления карточками умерших.
/// </summary>
[Route("api/deceased-records")]
[Tags("DeceasedRecords")]
public sealed class DeceasedRecordsSupportiveController : ApiControllerBase
{
    /// <summary>
    /// Возвращает расстояние от переданных координат до места захоронения.
    /// </summary>
    [HttpGet("{id:guid}/distance")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetDistanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetDistance(
        [FromRoute] Guid id,
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromServices] IGetDistanceUseCase getDistanceUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getDistanceUseCase.Execute(
            DeceasedRecordsMapping.ToDistanceQuery(id, latitude, longitude),
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Возвращает возраст на момент смерти.
    /// </summary>
    [HttpGet("{id:guid}/age-at-death")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetAgeAtDeathResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAgeAtDeath(
        [FromRoute] Guid id,
        [FromServices] IGetAgeAtDeathUseCase getAgeAtDeathUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getAgeAtDeathUseCase.Execute(
            DeceasedRecordsMapping.ToAgeAtDeathQuery(id),
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Проверяет, есть ли у карточки воспоминания.
    /// </summary>
    [HttpGet("{id:guid}/has-memories")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<HasMemoriesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HasMemories(
        [FromRoute] Guid id,
        [FromServices] IHasMemoriesUseCase hasMemoriesUseCase,
        CancellationToken cancellationToken)
    {
        var result = await hasMemoriesUseCase.Execute(
            DeceasedRecordsMapping.ToHasMemoriesQuery(id),
            cancellationToken);

        return FromResult(result);
    }
}