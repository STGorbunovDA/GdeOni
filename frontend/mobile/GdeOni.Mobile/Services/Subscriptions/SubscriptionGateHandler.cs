using System.Net;
using System.Text;
using System.Text.Json;

namespace GdeOni.Mobile.Services.Subscriptions;

/// <summary>
/// E22.6. DelegatingHandler, ловящий 403 с
/// <c>errorCode="subscription.required"</c> от бэкенда. Это случай,
/// когда подписка истекла между запусками приложения и юзер на
/// каком-нибудь экране внезапно получил 403 — переводим его на
/// SubscriptionRequiredPage, а не показываем непонятную ошибку.
///
/// Чтение тела ответа консумит stream, поэтому восстанавливаем его
/// новым StringContent — иначе Refit десериализация ниже по цепочке
/// получит пустой стрим.
/// </summary>
public sealed class SubscriptionGateHandler : DelegatingHandler
{
    private const string SubscriptionRequiredCode = "subscription.required";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Forbidden)
            return response;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!IsSubscriptionRequired(body))
            return RewindBody(response, body);

        // Передачу на UI-поток делаем fire-and-forget — handler возвращает
        // оригинальный 403, а Shell параллельно уже переключается на
        // paywall-страницу. Если приложение в этот момент не на main-thread
        // (background fetch и т.п.) — MainThread инвокер сам поставит в
        // очередь.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync("//subscription-required");
            }
            catch
            {
                // GoToAsync может бросить, если Shell в этот момент в
                // переходе. В таком случае при следующем тапе/перерисовке
                // юзер всё равно увидит paywall (на старте — через
                // PaywallChecker).
            }
        });

        return RewindBody(response, body);
    }

    private static bool IsSubscriptionRequired(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("errorCode", out var codeElement))
                return false;
            return string.Equals(codeElement.GetString(), SubscriptionRequiredCode, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static HttpResponseMessage RewindBody(HttpResponseMessage response, string body)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
        var rewound = new StringContent(body, Encoding.UTF8, mediaType);

        // Сохраняем оригинальные заголовки content (например, Content-Length).
        foreach (var header in response.Content.Headers)
        {
            if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
            rewound.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        response.Content = rewound;
        return response;
    }
}
