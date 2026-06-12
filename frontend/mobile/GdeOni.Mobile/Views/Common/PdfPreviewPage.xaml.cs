namespace GdeOni.Mobile.Views.Common;

/// <summary>
/// D35. Inline-просмотр PDF через WebView. URL — presigned-ссылка от
/// бэка (TTL 1 час). На Android 13+ Chrome WebView рендерит PDF
/// прямо в странице (не уходим из приложения). На более старых
/// устройствах WebView попробует скачать — но юзер может выйти и
/// открыть штатным "Открыть в браузере" (см. SupportDetailsPage,
/// этот fallback пока не делаем).
/// </summary>
[QueryProperty(nameof(Url), "url")]
[QueryProperty(nameof(FileName), "fileName")]
public partial class PdfPreviewPage : ContentPage
{
    private string _url = "";
    private string _fileName = "";

    public string Url
    {
        get => _url;
        set
        {
            _url = Uri.UnescapeDataString(value ?? "");
            if (!string.IsNullOrEmpty(_url))
            {
                Web.Source = _url;
                Web.Navigated += OnWebNavigated;
            }
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = Uri.UnescapeDataString(value ?? "");
            if (!string.IsNullOrEmpty(_fileName))
                FileNameLabel.Text = _fileName;
        }
    }

    public PdfPreviewPage()
    {
        InitializeComponent();
    }

    private void OnWebNavigated(object? sender, WebNavigatedEventArgs e)
    {
        Spinner.IsRunning = false;
        Spinner.IsVisible = false;
    }

    private async void OnCloseTapped(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
