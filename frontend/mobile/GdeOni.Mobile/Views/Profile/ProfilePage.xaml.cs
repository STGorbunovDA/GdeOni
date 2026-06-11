using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Profile;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;

    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // E22: подтягиваем профиль и подписку всегда — при возврате с
        // SubscriptionPage юзер мог отменить подписку / оформить новую,
        // нужно показать актуальный статус.
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
