using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AzureDesktop.Views;

public sealed partial class AppGwOverviewPage : AppGwPageBase
{
    public override string PageLabel => "Overview";
    public override string? ActiveNavTag => "AppGwOverview";
    protected override bool IsOverviewPage => true;

    public AppGwOverviewPage()
    {
        InitializeComponent();
    }

    protected override void OnDataLoaded()
    {
        PropertyCardStack.Children.Clear();
        AddPropertyCard([
            ("Name", ViewModel.Name),
            ("Location", ViewModel.Location),
            ("SKU Name", ViewModel.SkuName),
            ("SKU Tier", ViewModel.SkuTier),
            ("Capacity", ViewModel.SkuCapacity.ToString()),
            ("Operational State", ViewModel.OperationalState),
            ("Provisioning State", ViewModel.ProvisioningState),
            ("Resource ID", ViewModel.ResourceId),
        ]);
    }

    private void AddPropertyCard(List<(string Label, string Value)> properties)
    {
        var stack = new StackPanel { Spacing = 16 };

        for (int i = 0; i < properties.Count; i++)
        {
            if (i > 0)
            {
                stack.Children.Add(new Border
                {
                    Height = 1,
                    Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"]
                });
            }

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var glyph = FieldIcons.Map.GetValueOrDefault(properties[i].Label, "\uE946");
            var icon = new FontIcon
            {
                Glyph = glyph,
                FontSize = 14,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                VerticalAlignment = VerticalAlignment.Center,
            };

            var label = new TextBlock
            {
                Text = properties[i].Label,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(label, 1);

            var value = new TextBlock
            {
                Text = properties[i].Value,
                IsTextSelectionEnabled = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(value, 2);

            grid.Children.Add(icon);
            grid.Children.Add(label);
            grid.Children.Add(value);
            stack.Children.Add(grid);
        }

        var card = new Border
        {
            Padding = new Thickness(20),
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = stack,
        };

        PropertyCardStack.Children.Add(card);
    }
}
