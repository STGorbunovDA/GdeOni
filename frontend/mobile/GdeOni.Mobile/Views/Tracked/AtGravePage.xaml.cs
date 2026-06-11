using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class AtGravePage : ContentPage
{
    private readonly AtGraveViewModel _viewModel;
    private bool _initialGeoRequested;

    public AtGravePage(AtGraveViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Сразу показываем баннер про VPN, если он включён — до того,
        // как юзер вообще запросит координаты.
        _viewModel.RefreshVpnStatus();

        // При первом открытии экрана сразу пробуем получить координаты —
        // юзеру не надо тапать "Получить" каждый раз. Если откажет — он
        // увидит сообщение и сможет повторить вручную.
        if (!_initialGeoRequested)
        {
            _initialGeoRequested = true;
            await _viewModel.RequestLocationCommand.ExecuteAsync(null);
        }
    }
}
