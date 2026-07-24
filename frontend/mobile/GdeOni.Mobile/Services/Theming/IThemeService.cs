using GdeOni.Mobile.Shared.Theming;

namespace GdeOni.Mobile.Services.Theming;

/// <summary>
/// E27. Управление темой оформления (светлая / тёмная / как в системе).
/// </summary>
public interface IThemeService
{
    /// <summary>Текущий выбранный режим.</summary>
    ThemeMode Current { get; }

    /// <summary>
    /// Фактически применённая сейчас тема тёмная? Учитывает и явный выбор,
    /// и режим «как в системе» (тогда берётся системная тема Android).
    /// </summary>
    bool IsDarkTheme { get; }

    /// <summary>Применить и сохранить выбор пользователя.</summary>
    void Apply(ThemeMode mode);

    /// <summary>
    /// Быстрое переключение светлая↔тёмная от ТЕКУЩЕЙ фактической темы
    /// (зеркало web-кнопки солнце/луна). Всегда выставляет явный режим —
    /// «как в системе» после нажатия больше не следим (как на вебе).
    /// </summary>
    void ToggleLightDark();

    /// <summary>
    /// Прочитать сохранённый режим и применить его. Зовётся один раз на
    /// старте приложения (до создания окна).
    /// </summary>
    void Initialize();
}
