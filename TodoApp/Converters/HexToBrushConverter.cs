using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TodoApp.Converters;

/// <summary>Converts a hex colour string (e.g. "#FFF9C4") to a SolidColorBrush.</summary>
[ValueConversion(typeof(string), typeof(SolidColorBrush))]
public sealed class HexToBrushConverter : IValueConverter
{
    public static readonly HexToBrushConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                var colour = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(colour);
            }
        }
        catch { /* fall through */ }

        return new SolidColorBrush(Color.FromRgb(0xFF, 0xF9, 0xC4)); // default yellow
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
