using GdeOni.API.Authorization;
using GdeOni.API.Extensions;
using GdeOni.API.Models.App;
using GdeOni.API.Options;
using GdeOni.API.Response;
using GdeOni.Application.Abstractions.Features;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Subscriptions;
using GdeOni.Infrastructure.Payments;
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
[Tags("App")]
[Route("api/app")]
public sealed class AppController : ApiControllerBase
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
    [Authorize(Policy = AuthorizationPolicies.BasicAuthenticated)]
    [ProducesResponseType(typeof(ApiResponse<AppFeaturesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public IActionResult GetFeatures(
        [FromServices] IFeatureFlagService featureFlags,
        [FromServices] IFileStorage fileStorage,
        [FromServices] IOptionsSnapshot<SubscriptionOptions> subscriptionOptions,
        [FromServices] IOptionsSnapshot<YooKassaOptions> yooKassaOptions,
        [FromServices] IOptionsSnapshot<GeolocationOptions> geolocationOptions)
    {
        // D36: mediaBaseUrl приходит из MinioOptions.PublicBaseUrl и
        // отдаётся клиентам без bucket/key — каждый клиент сам строит
        // финальный URL. Это снимает проблему "один URL для всех клиентов",
        // когда mobile-эмулятор и web имеют разные хост-маппинги.
        //
        // F39: цена подписки — оттуда же, откуда её берёт CreatePayment.
        // Один источник правды: клиент показывает ровно ту сумму, которую
        // спишет платёжный провайдер.
        // D44: можно ли РЕАЛЬНО заплатить. Не IsConfigured, а
        // IsLivePaymentsEnabled — тестовый ключ YooKassa деньги не
        // принимает, для юзера это то же самое, что «оплата не
        // работает». Клиенты по флагу гасят кнопку оплаты и ведут
        // человека в обращение (оплата переводом).
        // Кламп окна геолокации в разумные границы (5..300 сек) — защита от
        // опечатки в конфиге (0 = моментальный, худший fix; гигантское =
        // человек ждёт вечно).
        var geoWindow = Math.Clamp(geolocationOptions.Value.AcquireWindowSeconds, 5, 300);
        // Порог ранней остановки: 0 = не останавливаться досрочно (собирать
        // всё окно); верхний предел бережём от абсурда.
        var geoTargetAccuracy = Math.Clamp(geolocationOptions.Value.TargetAccuracyMeters, 0, 1000);

        var response = new AppFeaturesResponse(
            featureFlags.IsSubscriptionEnabled,
            featureFlags.GracePeriodDaysAfterExpiry,
            fileStorage.GetMediaBaseUrl(),
            subscriptionOptions.Value.MonthlyPriceRub,
            yooKassaOptions.Value.IsLivePaymentsEnabled,
            geoWindow,
            geoTargetAccuracy);

        return response.ToOkResponse();
    }
}
