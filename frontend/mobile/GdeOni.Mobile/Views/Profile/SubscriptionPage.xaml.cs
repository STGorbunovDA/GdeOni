using GdeOni.Mobile.Services.Subscriptions;
using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Profile;

public partial class SubscriptionPage : ContentPage
{
    private readonly SubscriptionViewModel _viewModel;

    public SubscriptionPage(SubscriptionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // E22.7. Если юзер только что вернулся через deep link
        // gdeoni://payment/return — YooKassa могла ещё не прислать
        // webhook, поэтому первый GET вернёт PendingPayment. Запускаем
        // поллинг каждые 3 секунды до 30 секунд пока не Active.
        if (PaymentReturnState.ConsumeIfReturned())
        {
            await _viewModel.StartPollingIfPendingAsync();
            return;
        }

        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
