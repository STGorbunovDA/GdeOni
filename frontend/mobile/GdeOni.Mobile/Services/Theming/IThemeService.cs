using GdeOni.Mobile.Shared.Theming;

namespace GdeOni.Mobile.Services.Theming;

/// <summary>
/// E27. Управление темой оформления (светлая / тёмная / как в системе).
/// </summary>
public interface IThemeService
{
    /// <summary>Текущий выбранный режим.</summary>
    ThemeMode Current { get; }

    /// <summary>Применить и сохранить выбор пользователя.</summary>
    void Apply(ThemeMode mode);

    /// <summary>
    /// Прочитать сохранённый режим и применить его. Зовётся один раз на
    /// старте приложения (до создания окна).
    /// </summary>
    void Initialize();
}
