using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace GdeOni.Mobile;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
// E22.7. Deep link gdeoni://payment/return — после оплаты YooKassa
// возвращает юзера на этот URL. Android отдаёт intent сюда, мы
// перебрасываем на SubscriptionPage с поллингом статуса.
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "gdeoni",
    DataHost = "payment")]
// E23 C.2. Deep link gdeoni://deceased/{deceasedId} — отправляется из
// notification AnniversaryAlarmReceiver. MainActivity ловит, парсит ID,
// пушит на DeceasedDetailsPage. Если юзер не залогинен — попадёт на
// LoginPage и deep link потеряется (простой кейс, см. план).
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "gdeoni",
    DataHost = "deceased")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleDeepLink(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        // SingleTop launch mode + intent filter выше → если приложение
        // уже запущено и юзер вернулся через deep-link, новый intent
        // прилетает сюда (не в OnCreate). Без обработки здесь — глобально
        // невидимый event.
        HandleDeepLink(intent);
    }

    private static void HandleDeepLink(Intent? intent)
    {
        if (intent?.Data is null) return;
        if (!string.Equals(intent.Data.Scheme, "gdeoni", StringComparison.OrdinalIgnoreCase)) return;

        var host = intent.Data.Host;
        if (string.Equals(host, "payment", StringComparison.OrdinalIgnoreCase))
        {
            HandlePaymentReturnIntent(intent);
        }
        else if (string.Equals(host, "deceased", StringComparison.OrdinalIgnoreCase))
        {
            HandleDeceasedDeepLink(intent);
        }
    }

    private static void HandlePaymentReturnIntent(Intent intent)
    {
        // Shell.Current может быть null если приложение поднимается
        // через cold-start deep-link — навигацию выполнит AppShell сам
        // в OnAppearing после инициализации (см. PaymentReturnState
        // ниже). Если уже инициализирован — переходим сразу.
        Services.Subscriptions.PaymentReturnState.MarkReturned();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (Shell.Current is null) return;
            try
            {
                await Shell.Current.GoToAsync("//main/profile/subscription");
            }
            catch
            {
                // Если Shell ещё в переходе — флаг PaymentReturnState
                // подхватит SubscriptionPage.OnAppearing при следующем
                // запуске. Никакого UX-сбоя.
            }
        });
    }

    private static void HandleDeceasedDeepLink(Intent intent)
    {
        // URI вида gdeoni://deceased/{guid} — путь начинается с "/", дальше id.
        var path = intent.Data?.Path?.TrimStart('/');
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!Guid.TryParse(path, out _)) return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Shell ещё не инициализирован при cold-start: ждём один tick,
            // чтобы OnAppearing AppShell успел отработать (он сделает
            // навигацию на //main/tracked или paywall). После этого
            // пушим на конкретную карточку. Альтернатива — сохранять
            // pending deep link в state'е и применять из AppShell, но
            // для простого кейса это избыточно.
            for (var i = 0; i < 20 && Shell.Current is null; i++)
                await Task.Delay(100);
            if (Shell.Current is null) return;
            try
            {
                await Shell.Current.GoToAsync($"deceased-details?deceasedId={path}");
            }
            catch
            {
                // Если навигация не сработала (например, юзер на paywall'е
                // или login'е) — игнорируем. Юзер увидит карточку когда
                // дойдёт до tracked-вкладки.
            }
        });
    }
}
