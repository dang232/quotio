using System.Globalization;
using System.Windows.Data;

namespace Quotio.App.Converters;

public class BoolToStartStopTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRunning)
        {
            return isRunning ? "Stop Proxy" : "Start Proxy";
        }
        return "Start Proxy";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
