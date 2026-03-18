using GdeOni.Application.Deceased.Create.Model;
using GdeOni.Application.Deceased.Create.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

[ApiController]
[Route("api/deceasedRecords")]
public sealed class DeceasedsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateDeceasedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeceasedRequest request,
        [FromServices] ICreateDeceasedUseCase createDeceasedUseCase,
        CancellationToken cancellationToken = default)
    {
        var result = await createDeceasedUseCase.Execute(request, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Created($"/api/deceasedRecords/{result.Value.Id}", result.Value);
    }
}