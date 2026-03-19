using GdeOni.API.Extensions;
using GdeOni.API.Response;
using GdeOni.Application.Users.Create.Model;
using GdeOni.Application.Users.Create.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        [FromServices] ICreateUserUseCase createUserUseCase,
        CancellationToken cancellationToken)
    {
        var result = await createUserUseCase.Execute(request, cancellationToken);

        if (result.IsFailure)
            return result.Error.ToErrorResponse<CreateUserResponse>();
        return result.Value.ToCreatedResponse($"/api/users/{result.Value.Id}");
    }
}