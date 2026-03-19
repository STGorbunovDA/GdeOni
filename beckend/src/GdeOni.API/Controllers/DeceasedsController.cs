using GdeOni.API.Extensions;
using GdeOni.API.Response;
using GdeOni.Application.Deceased.Create.Model;
using GdeOni.Application.Deceased.Create.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

[ApiController]
[Route("api/deceasedRecords")]
public sealed class DeceasedsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeceasedRequest request,
        [FromServices] ICreateDeceasedUseCase createDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var result = await createDeceasedUseCase.Execute(request, cancellationToken);

        if (result.IsFailure)
            return result.Error.ToErrorResponse();

        return result.Value.ToCreatedResponse($"/api/deceasedRecords/{result.Value.Id}");
    }
}