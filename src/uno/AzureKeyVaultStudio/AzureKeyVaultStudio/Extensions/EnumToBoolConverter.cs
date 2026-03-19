using Microsoft.UI.Xaml.Data;

namespace AzureKeyVaultStudio.Extensions;

public partial class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Enum && parameter is string enumString)
        {
            var enumValue = Enum.Parse(value.GetType(), enumString);
            return value.Equals(enumValue);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        //if (value is bool isChecked && isChecked && parameter is string enumString)
        //{
        //    return Enum.Parse(targetType, enumString);
        //}
        return value;
    }
}


