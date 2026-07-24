using GdeOni.Mobile.Services.Theming;

namespace GdeOni.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;

        // E27. Применяем сохранённую тему до создания окна, чтобы первый
        // кадр рисовался сразу в нужной схеме (зеркало web-скрипта в
        // index.html, который ставит color-scheme до первой отрисовки).
        _services.GetRequiredService<IThemeService>().Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = _services.GetRequiredService<AppShell>();
        return new Window(shell);
    }
}
