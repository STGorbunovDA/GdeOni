using GdeOni.Application.Users.Create.Model;
using GdeOni.Application.Users.Create.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        [FromServices] ICreateUserUseCase createUserUseCase,
        CancellationToken cancellationToken = default)
    {
        var result = await createUserUseCase.Execute(request, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Created($"/api/users/{result.Value.Id}", result.Value);
    }
}