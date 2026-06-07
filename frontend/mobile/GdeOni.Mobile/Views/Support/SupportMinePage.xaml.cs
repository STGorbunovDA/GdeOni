using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Support;

public partial class SupportMinePage : ContentPage
{
    private readonly SupportMineViewModel _viewModel;

    public SupportMinePage(SupportMineViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Перезагружаем при каждом появлении — после создания нового
    /// обращения юзер возвращается сюда и должен сразу увидеть его
    /// в ленте без manual refresh.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadFirstPageAsync();
    }
}
