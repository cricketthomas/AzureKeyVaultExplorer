using Microsoft.UI.Xaml.Data;

namespace AzureKeyVaultStudio.Extensions;

public partial class DateTimeFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTimeOffset dto)
        {
            var format = parameter as string ?? "g";
            return dto.ToLocalTime().ToString(format);
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

