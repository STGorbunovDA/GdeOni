using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Subscriptions;

public partial class SubscriptionRequiredPage : ContentPage
{
    private readonly SubscriptionRequiredViewModel _viewModel;

    public SubscriptionRequiredPage(SubscriptionRequiredViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    /// <summary>
    /// F39. Цену тянем с бэка при показе экрана — она живёт в конфиге
    /// сервера, а не в строке клиента.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadPriceAsync();
    }

    /// <summary>
    /// E22.6. Системную кнопку Back с paywall'а игнорируем — выход
    /// только через "Выйти из аккаунта" или оплату подписки.
    /// </summary>
    protected override bool OnBackButtonPressed() => true;
}
