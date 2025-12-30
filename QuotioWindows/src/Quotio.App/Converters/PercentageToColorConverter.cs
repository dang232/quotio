using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Quotio.App.Converters;

public class PercentageToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            if (percentage < 20) return App.Current.Resources["QuotaLowBrush"];
            if (percentage < 50) return App.Current.Resources["QuotaMediumBrush"];
            return App.Current.Resources["QuotaHighBrush"];
        }
        return App.Current.Resources["QuotaHighBrush"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
