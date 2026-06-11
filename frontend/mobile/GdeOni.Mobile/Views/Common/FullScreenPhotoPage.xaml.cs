namespace GdeOni.Mobile.Views.Common;

/// <summary>
/// Простой полноэкранный просмотр фото. URL передаётся через
/// Shell QueryProperty "url" (url-encoded на стороне caller'а).
/// Тап по фото или ✕ — закрывают страницу (Shell pop).
/// </summary>
[QueryProperty(nameof(Url), "url")]
public partial class FullScreenPhotoPage : ContentPage
{
    private string _url = "";

    public string Url
    {
        get => _url;
        set
        {
            _url = Uri.UnescapeDataString(value ?? "");
            if (!string.IsNullOrEmpty(_url))
                Photo.Source = _url;
        }
    }

    public FullScreenPhotoPage()
    {
        InitializeComponent();
    }

    private async void OnCloseTapped(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
