using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Admin;

public partial class AdminUserDetailsPage : ContentPage
{
    private readonly AdminUserDetailsViewModel _viewModel;
    private bool _firstAppearing = true;

    public AdminUserDetailsPage(AdminUserDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// При первом OnAppearing данные грузит OnUserIdChanged (срабатывает
    /// после QueryProperty). При возврате со вложенных страниц (например,
    /// admin-user-tracked после снятия отслеживаний) явно перезагружаем
    /// чтобы TrackingCount/подписка/роль отражали свежее состояние.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_firstAppearing)
        {
            _firstAppearing = false;
            return;
        }
        await _viewModel.RefreshAsync();
    }
}
