using GdeOni.Application.Users.Create.Model;
using GdeOni.Application.Users.Create.Service;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ICreateUserService _createUserService;

    public UsersController(ICreateUserService createUserService)
    {
        _createUserService = createUserService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createUserService.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Created($"/api/users/{result.Value.Id}", result.Value);
    }
}