using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace TodoApp.Converters;

/// <summary>
/// Converts a URL string to a <see cref="BitmapImage"/> for display in an
/// <see cref="System.Windows.Controls.Image"/> element.
/// Returns null (no image) when the value is null, empty, or the URL cannot
/// be parsed / loaded.
/// </summary>
[ValueConversion(typeof(string), typeof(BitmapImage))]
public sealed class UriToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            var bmi = new BitmapImage();
            bmi.BeginInit();
            bmi.UriSource        = new Uri(url, UriKind.Absolute);
            bmi.CacheOption      = BitmapCacheOption.OnLoad;
            bmi.CreateOptions    = BitmapCreateOptions.IgnoreImageCache;
            bmi.DecodePixelWidth = 320;   // cap to reasonable size
            bmi.EndInit();
            return bmi;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
