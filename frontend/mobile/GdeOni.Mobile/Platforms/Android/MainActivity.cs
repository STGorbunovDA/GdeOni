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
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandlePaymentReturnIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        // SingleTop launch mode + intent filter выше → если приложение
        // уже запущено и юзер вернулся через deep-link, новый intent
        // прилетает сюда (не в OnCreate). Без обработки здесь — глобально
        // невидимый event.
        HandlePaymentReturnIntent(intent);
    }

    private static void HandlePaymentReturnIntent(Intent? intent)
    {
        if (intent?.Data is null) return;
        if (!string.Equals(intent.Data.Scheme, "gdeoni", StringComparison.OrdinalIgnoreCase)) return;
        if (!string.Equals(intent.Data.Host, "payment", StringComparison.OrdinalIgnoreCase)) return;

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
}
