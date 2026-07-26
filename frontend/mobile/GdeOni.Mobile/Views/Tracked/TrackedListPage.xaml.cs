using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class TrackedListPage : ContentPage
{
    private readonly TrackedListViewModel _viewModel;

    public TrackedListPage(TrackedListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Подгружаем список при возврате на вкладку — после создания
        // карточки в at-grave новый item должен появиться сразу.
        await _viewModel.LoadCommand.ExecuteAsync(null);
        // E22. Если вышла новая версия — показать диалог обновления поверх
        // экрана (один раз за запуск).
        await _viewModel.MaybePromptUpdateAsync();
        // F42. Попап «сегодня праздник» — один раз в день при заходе.
        await _viewModel.MaybeShowHolidayPopupAsync();
    }
}
