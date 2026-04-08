using AzureDesktop.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/AppIcon.ico");

        ContentFrame.Navigated += ContentFrame_Navigated;

        // Navigate to Home on startup
        ContentFrame.Navigate(typeof(HomePage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        var canGoBack = ContentFrame.CanGoBack;
        NavView.IsBackButtonVisible = canGoBack
            ? NavigationViewBackButtonVisible.Visible
            : NavigationViewBackButtonVisible.Collapsed;
        NavView.IsBackEnabled = canGoBack;

        // Keep the correct nav item selected for drill-down pages
        if (e.SourcePageType == typeof(SubscriptionsPage)
            || e.SourcePageType == typeof(SubscriptionDetailPage)
            || e.SourcePageType == typeof(ResourceGroupsPage)
            || e.SourcePageType == typeof(ResourceGroupDetailPage)
            || e.SourcePageType == typeof(ResourcesPage)
            || e.SourcePageType == typeof(FeaturesPage)
            || e.SourcePageType == typeof(FeatureDetailPage))
        {
            NavView.SelectedItem = NavView.MenuItems[1];
        }
        else if (e.SourcePageType == typeof(HomePage))
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not NavigationViewItem item)
            return;

        var tag = item.Tag?.ToString();
        var pageType = tag switch
        {
            "Home" => typeof(HomePage),
            "Subscriptions" => typeof(SubscriptionsPage),
            "ApplicationGateway" => typeof(ApplicationGatewayPage),
            _ => null
        };

        if (pageType is not null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.BackStack.Clear();
            ContentFrame.Navigate(pageType);
        }
    }
}
