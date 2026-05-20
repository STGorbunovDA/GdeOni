using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Subscriptions;

public partial class SubscriptionRequiredPage : ContentPage
{
    public SubscriptionRequiredPage(SubscriptionRequiredViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// E22.6. Системную кнопку Back с paywall'а игнорируем — выход
    /// только через "Выйти из аккаунта" или оплату подписки.
    /// </summary>
    protected override bool OnBackButtonPressed() => true;
}
