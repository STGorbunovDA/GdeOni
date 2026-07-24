namespace GdeOni.Mobile.Shared.Theming;

/// <summary>
/// E27. Режим оформления, выбранный пользователем. Зеркало web-ключа
/// <c>gdeoni-color-scheme</c> (значения auto/light/dark), чтобы поведение
/// приложения и сайта совпадало.
///
/// <see cref="System"/> — следовать системной теме Android («как в системе»),
/// это дефолт. <see cref="Light"/>/<see cref="Dark"/> — принудительный выбор.
/// </summary>
public enum ThemeMode
{
    System,
    Light,
    Dark,
}
