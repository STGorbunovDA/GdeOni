using GdeOni.Mobile.Shared.Theming;

namespace GdeOni.Mobile.Services.Theming;

/// <summary>
/// E27. Хранит выбор темы в Preferences (ключ зеркалит web —
/// <c>gdeoni-color-scheme</c>) и транслирует его в
/// <see cref="Application.UserAppTheme"/>, откуда все AppThemeBinding в XAML
/// переключаются разом — в том числе вживую, без перезапуска приложения.
///
/// Парсинг/сериализация режима вынесены в
/// <see cref="ThemeModeParser"/> (Shared, покрыто тестами); здесь только
/// MAUI-специфика: Preferences + UserAppTheme.
/// </summary>
public sealed class ThemeService : IThemeService
{
    public ThemeMode Current { get; private set; } = ThemeMode.System;

    public void Initialize()
    {
        var stored = Preferences.Default.Get(ThemeModeParser.StorageKey, string.Empty);
        Apply(ThemeModeParser.Parse(stored));
    }

    public void Apply(ThemeMode mode)
    {
        Current = mode;
        Preferences.Default.Set(
            ThemeModeParser.StorageKey,
            ThemeModeParser.ToStorageString(mode));

        // Application.Current доступен уже в конструкторе App (после
        // InitializeComponent), поэтому Initialize на старте отработает.
        // Проверка на null — на случай раннего/тестового вызова.
        if (Application.Current is { } app)
        {
            app.UserAppTheme = mode switch
            {
                ThemeMode.Light => AppTheme.Light,
                ThemeMode.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified, // «как в системе»
            };
        }
    }
}
