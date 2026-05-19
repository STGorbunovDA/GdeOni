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
        // Подтягиваем профиль каждый раз, когда возвращаемся на вкладку —
        // токен мог обновиться, имя пользователя могло измениться.
        if (string.IsNullOrEmpty(_viewModel.UserName))
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
