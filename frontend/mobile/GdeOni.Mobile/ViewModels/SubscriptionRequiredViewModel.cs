using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Auth;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// E22.6. ViewModel для глобального paywall'а. Юзер попадает сюда:
///   - после Login, если SubscriptionEnabled && !IsActiveNow && !IsAdmin;
///   - в середине сессии, если backend вернул 403 subscription.required
///     (см. SubscriptionGateHandler).
/// Из paywall возможен только переход на SubscriptionPage (оформить)
/// или выход из аккаунта.
/// </summary>
public partial class SubscriptionRequiredViewModel(
    IAuthService authService,
    IAppApi appApi) : ObservableObject
{
    [ObservableProperty]
    private string _title = "Нужна подписка";

    /// <summary>
    /// F39. Текст без суммы — цену подставляем после загрузки features
    /// (см. <see cref="LoadPriceAsync"/>). Раньше «49 ₽/мес» было вшито
    /// в строку, и смена тарифа означала бы: на экране одна сумма, а
    /// спишется другая.
    /// </summary>
    [ObservableProperty]
    private string _message =
        "Чтобы пользоваться приложением, оформите подписку. " +
        "Доступ ко всем функциям без ограничений.";

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// D44. Подключена ли онлайн-оплата. Пока false, кнопка «Оформить
    /// подписку» скрыта, а основным действием становится обращение
    /// в поддержку: оплата переводом, доступ админ выдаёт вручную.
    ///
    /// Стартовое значение false намеренно: показать рабочую кнопку и
    /// тут же её убрать хуже, чем наоборот.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaymentsUnavailable))]
    private bool _paymentsAvailable;

    public bool PaymentsUnavailable => !PaymentsAvailable;

    /// <summary>
    /// Тянет цену и доступность оплаты с бэка. Ошибку глушим: paywall
    /// должен открыться и без них — лучше без суммы, чем пустой экран
    /// или неверный тариф.
    /// </summary>
    public async Task LoadPriceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var envelope = await appApi.GetFeaturesAsync(cancellationToken);
            if (envelope.Result is null) return;

            // Старый бэк поля не отдаёт → null → оплата считается
            // недоступной (см. комментарий к DTO).
            PaymentsAvailable = envelope.Result.PaymentsAvailable ?? false;

            if (envelope.Result.MonthlyPriceRub is not decimal price) return;

            Message = PaymentsAvailable
                ? $"Чтобы пользоваться приложением, оформите подписку — {price:0.##} ₽/мес. " +
                  "Доступ ко всем функциям без ограничений."
                : $"Пробный период закончился. Для продолжения нужна подписка — {price:0.##} ₽/мес. " +
                  "Онлайн-оплата пока не подключена: напишите нам, подскажем как оплатить и откроем доступ.";
        }
        catch
        {
            // Сеть/бэк недоступны — оставляем текст без суммы.
        }
    }

    [RelayCommand]
    private async Task SubscribeAsync()
    {
        await Shell.Current.GoToAsync("subscription");
    }

    /// <summary>
    /// D44. Уводит в форму обращения с преднастроенной темой «Оплата» —
    /// человеку, которого только что отрезало от приложения, не надо
    /// ещё и выбирать тему и формулировать текст.
    /// </summary>
    [RelayCommand]
    private static async Task ContactSupportAsync()
    {
        await Shell.Current.GoToAsync("support-new?kind=Payment");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            IsBusy = true;
            await authService.LogoutAsync();
            await Shell.Current.GoToAsync("//login");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
