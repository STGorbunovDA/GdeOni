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
    /// Логинимся
    /// </summary>
    /// <param name="command"></param>
    /// <param name="loginUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        [FromServices] ILoginUseCase loginUseCase,
        CancellationToken cancellationToken)
    {
        var result = await loginUseCase.Execute(command, cancellationToken);
        return FromResult(result);
    }
}