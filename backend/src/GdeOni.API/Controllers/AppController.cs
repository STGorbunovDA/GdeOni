using GdeOni.API.Extensions;
using GdeOni.API.Models.App;
using GdeOni.API.Options;
using GdeOni.API.Response;
using GdeOni.Application.Abstractions.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GdeOni.API.Controllers;

/// <summary>
/// D17. Версионность приложения и feature flags. Используется
/// клиентами (mobile/web) при старте: проверка минимально-поддерживаемой
/// версии и получение глобальных флагов (например,
/// <see cref="AppFeaturesResponse.SubscriptionEnabled"/>).
/// </summary>
[ApiController]
[Route("api/app")]
public sealed class AppController : ControllerBase
{
    /// <summary>
    /// Информация о минимально-поддерживаемой и последней версии
    /// клиента. AllowAnonymous — иначе старый клиент с протухшим
    /// токеном не узнает, что пора обновляться.
    /// </summary>
    [HttpGet("version")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AppVersionResponse>), StatusCodes.Status200OK)]
    public IActionResult GetVersion(
        [FromServices] IOptionsSnapshot<AppVersionOptions> options)
    {
        var value = options.Value;
        var response = new AppVersionResponse(
            value.MinSupportedVersion,
            value.LatestVersion,
            value.ForceUpdateMessage,
            value.DownloadUrl);

        return response.ToOkResponse();
    }

    /// <summary>
    /// Глобальные фичефлаги. Требует аутентификации, чтобы избежать
    /// утечки операционных решений анонимам (открытие/закрытие
    /// коммерциализации и т.п.).
    /// </summary>
    [HttpGet("features")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AppFeaturesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public IActionResult GetFeatures(
        [FromServices] IFeatureFlagService featureFlags)
    {
        var response = new AppFeaturesResponse(
            featureFlags.IsSubscriptionEnabled,
            featureFlags.GracePeriodDaysAfterExpiry);

        return response.ToOkResponse();
    }
}
