using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Updates;

public partial class BlockingUpdatePage : ContentPage
{
    public BlockingUpdatePage(BlockingUpdateViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Блокируем системную кнопку Back: пользователь не должен иметь
    /// возможности уйти с blocking-update обратно в приложение
    /// (см. E22). Единственный путь — нажать "Скачать обновление".
    /// </summary>
    protected override bool OnBackButtonPressed() => true;
}
