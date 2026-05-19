using System.Globalization;
using GdeOni.Mobile.Services.Api.Models;

namespace GdeOni.Mobile.Converters;

/// <summary>
/// Конвертер: строка от backend ("Friend", "Parent", …) → русское название
/// для UI ("Друг", "Родитель", …). Используется в XAML биндингах.
/// </summary>
public sealed class RelationshipDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => RelationshipTypes.Display(value as string);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
