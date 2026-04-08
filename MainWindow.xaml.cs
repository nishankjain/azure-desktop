using AzureDesktop.Controls;
using AzureDesktop.ViewModels;
using AzureDesktop.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop;

public sealed partial class MainWindow : Window
{
    private const double CompactThreshold = 640;

    private SubscriptionItem? _activeSubscription;
    private readonly List<NavigationViewItem> _subscriptionNavItems = [];

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ContentFrame.Navigated += ContentFrame_Navigated;
        NavView.DisplayModeChanged += NavView_DisplayModeChanged;
        SizeChanged += MainWindow_SizeChanged;

        ContentFrame.Navigate(typeof(HomePage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        UpdatePaneMode(args.Size.Width);
    }

    private void UpdatePaneMode(double width)
    {
        if (width < CompactThreshold)
        {
            NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
            NavView.IsPaneOpen = false;
            PaneToggleButton.Visibility = Visibility.Visible;
        }
        else
        {
            NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
            PaneToggleButton.Visibility = Visibility.Collapsed;
        }
    }

    private void NavView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        PaneToggleButton.Visibility = args.DisplayMode == NavigationViewDisplayMode.Minimal
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        BackButton.Visibility = ContentFrame.CanGoBack
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Track active subscription from navigation parameter
        if (e.Parameter is SubscriptionItem sub)
        {
            _activeSubscription = sub;
        }
        else if (e.Parameter is NavigationContext ctx)
        {
            _activeSubscription = ctx.Subscription;
        }
        else if (e.SourcePageType == typeof(HomePage) || e.SourcePageType == typeof(SubscriptionsPage))
        {
            _activeSubscription = null;
        }

        UpdateSubscriptionNavItems(e.SourcePageType);
    }

    private void UpdateSubscriptionNavItems(Type pageType)
    {
        // Remove old subscription nav items
        foreach (var item in _subscriptionNavItems)
        {
            NavView.MenuItems.Remove(item);
        }

        _subscriptionNavItems.Clear();

        var isSubscriptionScope = _activeSubscription is not null && pageType != typeof(SubscriptionsPage);

        if (!isSubscriptionScope)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
            return;
        }

        // Add subscription context header
        var subHeader = CreateNavItem(_activeSubscription!.Name, "SubscriptionDetail", "\xE8CB");
        var resourceGroups = CreateNavItem("Resource Groups", "ResourceGroups", "\xE8B7");
        var manageTags = CreateNavItem("Manage Tags", "ManageTags", "\xE1CB");
        var manageLocks = CreateNavItem("Manage Locks", "ManageLocks", "\xE72E");
        var previewFeatures = CreateNavItem("Preview Features", "PreviewFeatures", "\xE7FC");
        var resourceProviders = CreateNavItem("Resource Providers", "ResourceProviders", "\xE74C");

        _subscriptionNavItems.AddRange([subHeader, resourceGroups, manageTags, manageLocks, previewFeatures, resourceProviders]);

        // Add separator + items after Home
        foreach (var item in _subscriptionNavItems)
        {
            NavView.MenuItems.Add(item);
        }

        // Highlight the active item
        NavigationViewItem? activeItem = pageType switch
        {
            var t when t == typeof(SubscriptionDetailPage) => subHeader,
            var t when t == typeof(ResourceGroupsPage) || t == typeof(ResourceGroupDetailPage) || t == typeof(ResourcesPage) => resourceGroups,
            var t when t == typeof(FeaturesPage) || t == typeof(FeatureDetailPage) => previewFeatures,
            var t when t == typeof(ResourceProvidersPage) || t == typeof(ResourceProviderDetailPage) => resourceProviders,
            _ => subHeader,
        };

        NavView.SelectedItem = activeItem;
    }

    private static NavigationViewItem CreateNavItem(string content, string tag, string glyph)
    {
        return new NavigationViewItem
        {
            Content = content,
            Tag = tag,
            Icon = new FontIcon { Glyph = glyph },
        };
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void PaneToggle_Click(object sender, RoutedEventArgs e)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not NavigationViewItem item)
        {
            return;
        }

        var tag = item.Tag?.ToString();

        // Handle subscription sub-nav items
        if (_activeSubscription is not null)
        {
            switch (tag)
            {
                case "SubscriptionDetail":
                    ContentFrame.Navigate(typeof(SubscriptionDetailPage), _activeSubscription);
                    return;
                case "ResourceGroups":
                    ContentFrame.Navigate(typeof(ResourceGroupsPage), new NavigationContext(_activeSubscription));
                    return;
                case "ManageTags":
                    _ = TagManagerDialog.ShowAsync($"/subscriptions/{_activeSubscription.Id}", Content.XamlRoot);
                    return;
                case "ManageLocks":
                    _ = LockManagerDialog.ShowAsync($"/subscriptions/{_activeSubscription.Id}", Content.XamlRoot);
                    return;
                case "PreviewFeatures":
                    ContentFrame.Navigate(typeof(FeaturesPage), _activeSubscription);
                    return;
                case "ResourceProviders":
                    ContentFrame.Navigate(typeof(ResourceProvidersPage), _activeSubscription);
                    return;
            }
        }

        // Handle top-level nav items
        if (tag == "Home" && ContentFrame.CurrentSourcePageType != typeof(HomePage))
        {
            _activeSubscription = null;
            ContentFrame.BackStack.Clear();
            ContentFrame.Navigate(typeof(HomePage));
        }
    }
}
