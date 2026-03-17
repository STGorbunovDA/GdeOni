using GdeOni.Application.Deceased.Create.Model;
using GdeOni.Application.Deceased.Create.Service;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

[ApiController]
[Route("api/deceased")]
public sealed class DeceasedController : ControllerBase
{
    private readonly ICreateDeceasedService _createDeceasedService;

    public DeceasedController(ICreateDeceasedService createDeceasedService)
    {
        _createDeceasedService = createDeceasedService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateDeceasedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeceasedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createDeceasedService.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Created($"/api/deceased/{result.Value.Id}", result.Value);
    }
}