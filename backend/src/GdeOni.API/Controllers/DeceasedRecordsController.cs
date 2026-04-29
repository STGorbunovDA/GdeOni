using GdeOni.API.Extensions;
using GdeOni.API.Mappers;
using GdeOni.API.Models.DeceasedRecords;
using GdeOni.API.Response;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.Model;
using GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Application.DeceasedRecords.Commands.Create.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Delete.Model;
using GdeOni.Application.DeceasedRecords.Commands.Delete.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Update.Model;
using GdeOni.Application.DeceasedRecords.Commands.Update.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetById.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер для управления карточками умерших.
/// </summary>
[Route("api/deceased-records")]
public sealed class DeceasedRecordsController : ApiControllerBase
{
    /// <summary>
    /// Создает новую карточку умершего.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CreateDeceasedResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeceasedRequest request,
        [FromServices] ICreateDeceasedUseCase createDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var command = request.ToCreateCommand();
        var result = await createDeceasedUseCase.Execute(command, cancellationToken);

        return FromResult(
            result,
            value => value.ToCreatedResponse($"/api/deceased-records/{value.Id}"));
    }

    /// <summary>
    /// Создаёт карточку умершего «у могилы» — атомарно вместе с автотрекингом
    /// и местом захоронения по GPS-координатам. Главный сценарий мобильного клиента.
    /// </summary>
    [HttpPost("at-grave")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AddDeceasedAtGraveResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddAtGrave(
        [FromBody] AddDeceasedAtGraveRequest request,
        [FromServices] IAddDeceasedAtGraveUseCase addDeceasedAtGraveUseCase,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await addDeceasedAtGraveUseCase.Execute(command, cancellationToken);

        return FromResult(
            result,
            value => value.ToCreatedResponse($"/api/deceased-records/{value.DeceasedId}"));
    }

    /// <summary>
    /// Возвращает список всех карточек умерших с пагинацией и фильтрацией.
    /// Доступно только администраторам.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DeceasedListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllDeceasedRequest request,
        [FromServices] IGetAllDeceasedUseCase getAllDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var query = request.ToQuery();
        var result = await getAllDeceasedUseCase.Execute(query, cancellationToken);

        return FromResult(result, value => value.ToDto().ToOkResponse());
    }

    /// <summary>
    /// Возвращает карточку умершего по идентификатору.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DeceasedDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromServices] IGetDeceasedByIdUseCase getDeceasedByIdUseCase,
        CancellationToken cancellationToken)
    {
        var query = new GetDeceasedByIdQuery(id);
        var result = await getDeceasedByIdUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Обновляет основную информацию карточки умершего.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateDeceasedRequest request,
        [FromServices] IUpdateDeceasedUseCase updateDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id);
        var result = await updateDeceasedUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Удаляет карточку умершего.
    /// Доступно только администраторам.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        [FromServices] IDeleteDeceasedUseCase deleteDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var command = new DeleteDeceasedCommand(id);
        var result = await deleteDeceasedUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }
}