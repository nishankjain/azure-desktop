using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace AzureDesktop.Converters;

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        var hasValue = value is not null && (value is not string s || !string.IsNullOrEmpty(s));
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
