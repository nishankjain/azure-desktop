using Microsoft.UI.Xaml.Data;

namespace AzureDesktop.Converters;

public sealed class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        return value is not null && (value is not string s || !string.IsNullOrEmpty(s));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
