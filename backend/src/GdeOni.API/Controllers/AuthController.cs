using GdeOni.API.Models.Auth;
using GdeOni.API.Models.Users;
using GdeOni.API.Response;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Application.Auth.Login.UseCase;
using GdeOni.Application.Auth.Logout.Model;
using GdeOni.Application.Auth.Logout.UseCase;
using GdeOni.Application.Auth.Refresh.Model;
using GdeOni.Application.Auth.Refresh.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер авторизации.
/// </summary>
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    /// <summary>
    /// Выполняет вход пользователя по email и паролю.
    /// Возвращает access token и refresh token.
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

    /// <summary>
    /// Обновляет access token по refresh token.
    /// Старый refresh token отзывается, выдаётся новая пара.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<RefreshResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        [FromServices] IRefreshUseCase refreshUseCase,
        CancellationToken cancellationToken)
    {
        var command = new RefreshCommand(request.RefreshToken);
        var result = await refreshUseCase.Execute(command, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Отзывает refresh token. Идемпотентен:
    /// для несуществующего/уже отозванного токена возвращает 204 без ошибки.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        [FromServices] ILogoutUseCase logoutUseCase,
        CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(request.RefreshToken);
        var result = await logoutUseCase.Execute(command, cancellationToken);
        return FromUnitResult(result);
    }
}
