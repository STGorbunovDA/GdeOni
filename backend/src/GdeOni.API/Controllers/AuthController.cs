using GdeOni.API.Authorization;
using GdeOni.API.Mappers;
using GdeOni.API.Models.Auth;
using GdeOni.API.Models.Users;
using GdeOni.API.RateLimiting;
using GdeOni.API.Response;
using GdeOni.Application.Auth.ConfirmEmail.UseCase;
using GdeOni.Application.Auth.ForgotPassword.UseCase;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Application.Auth.Login.UseCase;
using GdeOni.Application.Auth.Logout.UseCase;
using GdeOni.Application.Auth.Refresh.Model;
using GdeOni.Application.Auth.Refresh.UseCase;
using GdeOni.Application.Auth.ResendConfirmation.UseCase;
using GdeOni.Application.Auth.ResetPassword.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GdeOni.API.Controllers;

/// <summary>
/// Контроллер авторизации.
/// </summary>
[Tags("Auth")]
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    /// <summary>
    /// Выполняет вход пользователя по email и паролю.
    /// Возвращает access token и refresh token.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] ILoginUseCase loginUseCase,
        CancellationToken cancellationToken)
    {
        var result = await loginUseCase.Execute(request.ToCommand(), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// D43. Запрашивает ссылку восстановления пароля на указанный email.
    /// </summary>
    /// <remarks>
    /// Всегда возвращает 200, даже если такого пользователя нет. Это
    /// сделано намеренно: иначе по коду ответа можно было бы перебором
    /// выяснить, какие адреса зарегистрированы в сервисе. Клиенту в любом
    /// случае показываем «если адрес зарегистрирован, письмо отправлено».
    /// </remarks>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] IForgotPasswordUseCase forgotPasswordUseCase,
        CancellationToken cancellationToken)
    {
        var result = await forgotPasswordUseCase.Execute(request.ToCommand(), cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// D43. Устанавливает новый пароль по токену из письма.
    /// </summary>
    /// <remarks>
    /// Токен одноразовый и с ограниченным сроком жизни. Успешный сброс
    /// закрывает все активные сессии пользователя на всех устройствах.
    /// </remarks>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        [FromServices] IResetPasswordUseCase resetPasswordUseCase,
        CancellationToken cancellationToken)
    {
        var result = await resetPasswordUseCase.Execute(request.ToCommand(), cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// D45. Подтверждает адрес email по токену из письма.
    /// </summary>
    /// <remarks>
    /// Токен одноразовый и с ограниченным сроком жизни. Анонимный —
    /// подтверждением личности служит сам токен. Повторный клик по уже
    /// использованной ссылке отвечает успехом (адрес уже подтверждён).
    /// </remarks>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        [FromServices] IConfirmEmailUseCase confirmEmailUseCase,
        CancellationToken cancellationToken)
    {
        var result = await confirmEmailUseCase.Execute(request.ToCommand(), cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// D45. Повторно отправляет письмо с подтверждением email.
    /// </summary>
    /// <remarks>
    /// Всегда возвращает 200, даже если такого пользователя нет или адрес
    /// уже подтверждён — как и forgot-password, чтобы по ответу нельзя было
    /// перебором выяснить, кто зарегистрирован. Анонимный: зовётся и с
    /// экрана «проверьте почту» (новый юзер ещё не вошёл), и из баннера
    /// (клиент подставляет email текущего юзера сам).
    /// </remarks>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendConfirmation(
        [FromBody] ResendConfirmationRequest request,
        [FromServices] IResendEmailConfirmationUseCase resendUseCase,
        CancellationToken cancellationToken)
    {
        var result = await resendUseCase.Execute(request.ToCommand(), cancellationToken);
        return FromUnitResult(result);
    }

    /// <summary>
    /// Обновляет access token по refresh token.
    /// Старый refresh token отзывается, выдаётся новая пара.
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ApiResponse<RefreshResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        [FromServices] IRefreshUseCase refreshUseCase,
        CancellationToken cancellationToken)
    {
        var result = await refreshUseCase.Execute(request.ToCommand(), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Отзывает refresh token текущего пользователя. Идемпотентен:
    /// для несуществующего, уже отозванного или чужого токена возвращает
    /// 204 без ошибки (одинаковый ответ скрывает существование чужих токенов).
    /// </summary>
    [HttpPost("logout")]
    [Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        [FromServices] ILogoutUseCase logoutUseCase,
        CancellationToken cancellationToken)
    {
        var result = await logoutUseCase.Execute(request.ToCommand(), cancellationToken);
        return FromUnitResult(result);
    }
}
