using GdeOni.API.Response;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.Model;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D16. Эндпоинт для входящих webhook'ов от платёжных провайдеров.
/// AllowAnonymous: запросы приходят от YooKassa, не от юзера. Auth
/// делается через pull-проверку статуса платежа в самом провайдере
/// (см. <see cref="GdeOni.Infrastructure.Payments.YooKassaPaymentProvider.VerifyWebhookAsync"/>).
///
/// TODO D16.5: добавить middleware с IP-whitelist YooKassa
/// (185.71.76.0/27, 185.71.77.0/27, 77.75.153.0/25, ...).
/// </summary>
[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ApiControllerBase
{
    /// <summary>
    /// Webhook от YooKassa: уведомление о смене статуса платежа.
    /// Тело — JSON по схеме YooKassa (см.
    /// https://yookassa.ru/developers/using-api/webhooks).
    /// </summary>
    [HttpPost("yookassa/webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> YooKassaWebhook(
        [FromServices] IProcessPaymentWebhookUseCase processWebhook,
        CancellationToken cancellationToken)
    {
        // Читаем сырое тело: парсинг + verify внутри use case /
        // payment provider. Контроллер не должен знать структуру
        // YooKassa payload.
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        // У YooKassa нет фиксированного "signature" заголовка — оставляем
        // null, провайдер использует pull-verify. На случай других
        // провайдеров с HMAC — заглядывает в X-Signature.
        var signatureHeader = Request.Headers.TryGetValue("X-Signature", out var sig)
            ? sig.ToString()
            : null;

        var result = await processWebhook.Execute(
            new ProcessPaymentWebhookCommand(payload, signatureHeader),
            cancellationToken);

        return FromUnitResult(result);
    }
}
