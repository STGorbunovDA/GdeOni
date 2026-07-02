using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Subscriptions;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// E22. Страница "Подписка": отображает текущий статус + кнопки
/// "Оформить" / "Отменить" / открытие checkout-URL.
///
/// Состояния (взаимоисключающие, проверяются в порядке):
///   1) HasComplimentaryAccess — блок "Бесплатный доступ от админа",
///      кнопок нет (D22 — управляется только админом).
///   2) Status=Active — paid-подписка, кнопка "Отменить".
///   3) Status=Trial — пробный период, кнопка "Оформить подписку".
///   4) Status=Cancelled & ExpiresAt&gt;now — paid-period дорабатывает,
///      кнопка "Оформить снова".
///   5) Иначе (Expired/None) — кнопка "Оформить подписку".
/// </summary>
public partial class SubscriptionViewModel(ISubscriptionsApi subscriptionsApi) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// E22.7. Активный поллинг "ждём подтверждения оплаты": показываем
    /// ActivityIndicator и подсказку, пока статус не сменится с
    /// PendingPayment на Active (или истечёт окно поллинга).
    /// </summary>
    [ObservableProperty]
    private bool _isPollingPayment;

    private CancellationTokenSource? _pollingCts;
    private const int PollingIntervalMs = 3000;
    private const int PollingMaxAttempts = 10; // 10 × 3s = 30 секунд

    // ───────── Текущее состояние из API ─────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusTitle))]
    [NotifyPropertyChangedFor(nameof(StatusDescription))]
    [NotifyPropertyChangedFor(nameof(ShowComplimentaryBlock))]
    [NotifyPropertyChangedFor(nameof(ShowSubscribeButton))]
    [NotifyPropertyChangedFor(nameof(ShowCancelButton))]
    [NotifyPropertyChangedFor(nameof(SubscribeButtonText))]
    private MySubscriptionResponse? _current;

    public bool ShowComplimentaryBlock => Current?.HasComplimentaryAccess == true;

    // На mobile отмена подписки не нужна — управление активной подпиской
    // делается через web/админку. На trial кнопок тоже нет: у юзера и так
    // активный доступ, оформлять/отменять до конца trial бессмысленно.
    public bool ShowCancelButton => false;

    // Кнопка "Оформить" показывается только когда подписка действительно
    // не активна: Cancelled, Expired, NeedSubscription (нет статуса).
    // На Trial, Active и PendingPayment — скрыта (у юзера и так доступ
    // или он уже в процессе оплаты).
    public bool ShowSubscribeButton =>
        Current is { HasComplimentaryAccess: false }
        && Current.Status is not "Active"
        && Current.Status is not "Trial"
        && Current.Status is not "PendingPayment";

    public string SubscribeButtonText => Current?.Status switch
    {
        "Cancelled" => "Оформить снова",
        _ => "Оформить подписку",
    };

    public string StatusTitle
    {
        get
        {
            if (Current is null) return string.Empty;
            if (Current.HasComplimentaryAccess)
                return "Бесплатный доступ от администратора";

            return Current.Status switch
            {
                "Trial" => "Пробный период",
                "Active" => "Подписка активна",
                "PendingPayment" => "Ожидаем подтверждения оплаты",
                "Cancelled" => "Подписка отменена",
                "Expired" => "Подписка истекла",
                _ => "Подписка не активна",
            };
        }
    }

    public string StatusDescription
    {
        get
        {
            if (Current is null) return string.Empty;

            if (Current.HasComplimentaryAccess)
            {
                var noteSuffix = string.IsNullOrWhiteSpace(Current.ComplimentaryAccessNote)
                    ? string.Empty
                    : $"\nПричина: {Current.ComplimentaryAccessNote}";

                if (Current.ComplimentaryAccessUntilUtc is null)
                    return "Доступ предоставлен бессрочно." + noteSuffix;

                var localUntil = Current.ComplimentaryAccessUntilUtc.Value.ToLocalTime();
                return $"Действует до {localUntil:dd.MM.yyyy} ({Current.DaysUntilExpiry} дн.)" + noteSuffix;
            }

            return Current.Status switch
            {
                "Trial" or "Active" or "Cancelled" or "PendingPayment" when Current.ExpiresAtUtc is { } expiry =>
                    $"До {expiry.ToLocalTime():dd.MM.yyyy} ({Current.DaysUntilExpiry} дн.)",
                "Expired" => "Срок подписки закончился. Оформите снова, чтобы продолжить пользоваться приложением.",
                _ => "Оформите подписку для полного доступа.",
            };
        }
    }

    // ───────── Команды ─────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            // D16 pull-fallback: перед каждым GetMy просим бэк подтянуть
            // свежий статус у YooKassa. Идемпотентно, no-op когда
            // синхронизировать нечего. Ошибку глотаем — GetMy отдаст
            // текущий статус как есть.
            try { await subscriptionsApi.SyncAsync(); }
            catch { /* игнорируем — упадёт GetMy если нужно */ }
            var envelope = await subscriptionsApi.GetMyAsync();
            Current = envelope.Result;
            if (envelope.Result is null)
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить данные подписки.";
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (HttpRequestException httpEx)
        {
            ErrorMessage = $"Сетевая ошибка: {httpEx.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// E22.7. Запуск поллинга если статус PendingPayment. Дёргаем
    /// /me/subscription каждые 3 секунды до 10 раз; останавливаемся
    /// как только статус сменился (webhook от YooKassa дошёл) или
    /// истекло окно. Если уже не Pending — no-op.
    ///
    /// Вызывается из <c>SubscriptionPage.OnAppearing</c> когда юзер
    /// вернулся через deep link (см. <see cref="PaymentReturnState"/>).
    /// </summary>
    public async Task StartPollingIfPendingAsync()
    {
        // Отменяем предыдущий поллинг (если юзер быстро ушёл-вернулся).
        _pollingCts?.Cancel();
        _pollingCts = new CancellationTokenSource();
        var ct = _pollingCts.Token;

        // Первая загрузка — синхронно. Если уже Active — поллить нечего.
        await LoadAsync();
        if (Current is null || Current.Status != "PendingPayment")
            return;

        IsPollingPayment = true;
        try
        {
            for (var attempt = 0; attempt < PollingMaxAttempts; attempt++)
            {
                try
                {
                    await Task.Delay(PollingIntervalMs, ct);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (ct.IsCancellationRequested) return;

                await LoadAsync();
                if (Current?.Status != "PendingPayment")
                    return;
            }
            // Окно истекло, статус всё ещё Pending — webhook задерживается.
            // Подсказка юзеру: можно сделать pull-to-refresh вручную.
            ErrorMessage = "Подтверждение оплаты задерживается. Потяните вниз для обновления через минуту.";
        }
        finally
        {
            IsPollingPayment = false;
        }
    }

    [RelayCommand]
    private async Task SubscribeAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var envelope = await subscriptionsApi.CreatePaymentAsync(new CreatePaymentRequest("Monthly"));
            if (envelope.Result is null || string.IsNullOrWhiteSpace(envelope.Result.CheckoutUrl))
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось создать платёж.";
                return;
            }

            // Открываем YooKassa checkout во внешнем браузере. После оплаты
            // юзер вручную вернётся в приложение и нажмёт "Обновить" —
            // backend webhook к этому моменту уже активирует подписку.
            await Launcher.Default.OpenAsync(new Uri(envelope.Result.CheckoutUrl));
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (HttpRequestException httpEx)
        {
            ErrorMessage = $"Сетевая ошибка: {httpEx.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelSubscriptionAsync()
    {
        var page = Shell.Current?.CurrentPage;
        if (page is not null)
        {
            var confirmed = await page.DisplayAlertAsync(
                "Отменить подписку?",
                "Доступ сохранится до конца текущего оплаченного периода. " +
                "Автопродления не будет — после окончания нужно будет оформить снова.",
                "Отменить подписку",
                "Не отменять");
            if (!confirmed) return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await subscriptionsApi.CancelAsync();
            // Перечитаем актуальный статус (Status=Cancelled).
            await LoadAsync();
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (HttpRequestException httpEx)
        {
            ErrorMessage = $"Сетевая ошибка: {httpEx.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
