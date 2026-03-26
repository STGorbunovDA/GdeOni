using GdeOni.API.Response;
using GdeOni.Application.DeceasedRecords.Create.Model;
using GdeOni.Application.DeceasedRecords.Create.UseCase;
using GdeOni.Application.DeceasedRecords.GetAll.Model;
using GdeOni.Application.DeceasedRecords.GetAll.UseCase;
using GdeOni.Application.DeceasedRecords.GetById.Model;
using GdeOni.Application.DeceasedRecords.GetById.UseCase;
using GdeOni.Application.DeceasedRecords.Update.Model;
using GdeOni.Application.DeceasedRecords.Update.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

[Route("api/deceased-records")]
public sealed class DeceasedRecordsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateDeceasedResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeceasedRequest request,
        [FromServices] ICreateDeceasedUseCase createDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var result = await createDeceasedUseCase.Execute(request, cancellationToken);

        return FromResult(
            result,
            value => Created($"/api/deceased-records/{value.Id}",
                ApiResponse<CreateDeceasedResponse>.Success(value)));
    }
    
    [HttpGet] // TODO добавить фильтрацию
    [ProducesResponseType(typeof(ApiResponse<List<DeceasedListItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromServices] IGetAllDeceasedUseCase getAllDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getAllDeceasedUseCase.Execute(cancellationToken);
        return FromResult(result);
    }
    
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DeceasedDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DeceasedDetailsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IGetDeceasedByIdUseCase getDeceasedByIdUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getDeceasedByIdUseCase.Execute(id, cancellationToken);
        return FromResult(result);
    }
    
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateDeceasedResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UpdateDeceasedResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDeceasedRequest request,
        [FromServices] IUpdateDeceasedUseCase updateDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        request.Id = id;

        var result = await updateDeceasedUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }
}