using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace Quotio.App.Converters;

public class BoolToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRunning)
        {
            return isRunning ? PackIconKind.Stop : PackIconKind.Play;
        }
        return PackIconKind.Play;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
