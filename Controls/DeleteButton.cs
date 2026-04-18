using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AzureDesktop.Controls;

public sealed class DeleteButton : Button
{
    public DeleteButton()
    {
        Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        Content = new FontIcon { Glyph = "\uE74D", FontSize = 14 };
        AutomationProperties.SetName(this, "Delete");
        ToolTipService.SetToolTip(this, "Delete");

        Resources["AccentButtonBackground"] = new SolidColorBrush(ColorHelper.FromArgb(255, 196, 43, 28));
        Resources["AccentButtonBackgroundPointerOver"] = new SolidColorBrush(ColorHelper.FromArgb(255, 212, 59, 44));
        Resources["AccentButtonBackgroundPressed"] = new SolidColorBrush(ColorHelper.FromArgb(255, 163, 35, 22));
        Resources["AccentButtonBorderBrush"] = new SolidColorBrush(Colors.Transparent);
        Resources["AccentButtonBorderBrushPointerOver"] = new SolidColorBrush(Colors.Transparent);
        Resources["AccentButtonBorderBrushPressed"] = new SolidColorBrush(Colors.Transparent);
    }
}
