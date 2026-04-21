using GdeOni.API.Models;
using GdeOni.API.Models.Users;
using GdeOni.API.Response;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Application.Auth.Login.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Авторизация
/// </summary>
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    /// <summary>
    /// Логин
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] ILoginUseCase loginUseCase,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await loginUseCase.Execute(command, cancellationToken);
        return FromResult(result);
    }
}