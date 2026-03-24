using GdeOni.API.Response;
using GdeOni.Application.Deceased.Create.Model;
using GdeOni.Application.Deceased.Create.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

[Route("api/deceased-records")]
public sealed class DeceasedRecordsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateDeceasedResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CreateDeceasedResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreateDeceasedResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CreateDeceasedResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<CreateDeceasedResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeceasedRequest request,
        [FromServices] ICreateDeceasedUseCase createDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var result = await createDeceasedUseCase.Execute(request, cancellationToken);

        return FromResult(
            result,
            value => Created($"/api/deceased-records/{value.Id}", ApiResponse<CreateDeceasedResponse>.Success(value)));
    }
}