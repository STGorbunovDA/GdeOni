using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// E22. ViewModel для BlockingUpdatePage. Состояние выставляется
/// один раз при навигации (через QueryProperty) и не меняется —
/// единственное доступное действие "Скачать обновление".
/// </summary>
public partial class BlockingUpdateViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private string _title = "Требуется обновление";

    [ObservableProperty]
    private string _message =
        "Ваша версия приложения больше не поддерживается. " +
        "Скачайте новую версию с сайта, чтобы продолжить пользоваться приложением.";

    [ObservableProperty]
    private string? _downloadUrl;

    [ObservableProperty]
    private string _installInstruction =
        "Перед установкой разрешите Android устанавливать приложения из неизвестных источников: " +
        "Настройки → Безопасность → Установка из неизвестных источников.";

    [ObservableProperty]
    private bool _hasDownloadUrl;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("downloadUrl", out var url) && url is string urlStr && !string.IsNullOrWhiteSpace(urlStr))
        {
            DownloadUrl = urlStr;
            HasDownloadUrl = true;
        }

        if (query.TryGetValue("message", out var msg) && msg is string msgStr && !string.IsNullOrWhiteSpace(msgStr))
        {
            Message = msgStr;
        }
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(DownloadUrl))
            return;

        try
        {
            await Launcher.Default.OpenAsync(new Uri(DownloadUrl));
        }
        catch
        {
            // Launcher.OpenAsync кидает если URI невалиден. Показывать
            // ошибку юзеру некуда (страница без footer'а под error label) —
            // молча игнорируем, юзер увидит что браузер не открылся и
            // попробует снова.
        }
    }
}
