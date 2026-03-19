using System;
using Avalonia.Data.Converters;
using System.Globalization;

namespace KeyVaultExplorer.Resources;

public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            var calculatedWidth = width * 0.25;
            var minWidth = calculatedWidth < 310 ? 310 : calculatedWidth;
            return minWidth > 550 ? 550 : minWidth;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}