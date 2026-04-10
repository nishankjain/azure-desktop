using AzureDesktop.ViewModels;
using AzureDesktop.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop;

public sealed partial class MainWindow : Window
{
    private const double CompactThreshold = 640;

    private SubscriptionItem? _activeSubscription;
    private NavigationContext? _activeNavContext;
    private readonly List<NavigationViewItem> _contextNavItems = [];

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ContentFrame.Navigated += ContentFrame_Navigated;
        NavView.DisplayModeChanged += NavView_DisplayModeChanged;
        SizeChanged += MainWindow_SizeChanged;

        ContentFrame.Navigate(typeof(HomePage));
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

    private string? _lastAppGwNavTag;

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        BackButton.Visibility = ContentFrame.CanGoBack
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Trim back stack to prevent unbounded memory growth
        while (ContentFrame.BackStack.Count > 5)
        {
            ContentFrame.BackStack.RemoveAt(0);
        }

        _lastAppGwNavTag = null;

        if (e.Parameter is SubscriptionItem sub)
        {
            _activeSubscription = sub;
            _activeNavContext = null;
        }
        else if (e.Parameter is (NavigationContext appGwCtx, AppGwSection section))
        {
            _activeSubscription = appGwCtx.Subscription;
            _activeNavContext = appGwCtx;
            _lastAppGwNavTag = section switch
            {
                AppGwSection.BackendPools => "AppGwBackendPools",
                AppGwSection.BackendSettings => "AppGwBackendSettings",
                AppGwSection.FrontendIP => "AppGwFrontendIP",
                AppGwSection.PrivateLink => "AppGwPrivateLink",
                AppGwSection.SslSettings => "AppGwSsl",
                AppGwSection.Listeners => "AppGwListeners",
                AppGwSection.RoutingRules => "AppGwRoutingRules",
                AppGwSection.RewriteSets => "AppGwRewriteSets",
                AppGwSection.HealthProbes => "AppGwHealthProbes",
                AppGwSection.Configuration => "AppGwConfig",
                AppGwSection.Waf => "AppGwWaf",
                AppGwSection.JwtValidation => "AppGwJwt",
                _ => "AppGwOverview",
            };
        }
        else if (e.Parameter is NavigationContext ctx)
        {
            _activeSubscription = ctx.Subscription;
            _activeNavContext = ctx;
        }
        else if (e.Parameter is (NavigationContext subDetailCtx, string _))
        {
            _activeSubscription = subDetailCtx.Subscription;
            _activeNavContext = subDetailCtx;
        }
        else if (e.Parameter is (string, List<string>, NavigationContext tagLockCtx))
        {
            _activeSubscription = tagLockCtx.Subscription;
            _activeNavContext = tagLockCtx;
        }
        else if (e.Parameter is (string, List<string>, SubscriptionItem tagLockSub))
        {
            _activeSubscription = tagLockSub;
            _activeNavContext = null;
        }
        else if (e.SourcePageType == typeof(HomePage) || e.SourcePageType == typeof(SubscriptionsPage))
        {
            _activeSubscription = null;
            _activeNavContext = null;
        }

        UpdateContextNav(e.SourcePageType);
    }

    private void UpdateContextNav(Type pageType)
    {
        foreach (var item in _contextNavItems)
        {
            NavView.MenuItems.Remove(item);
        }

        _contextNavItems.Clear();

        if (_activeSubscription is null)
        {
            NavView.SelectedItem = null;
            return;
        }

        // Show only actions for the current scope
        // Tags/Locks pages inherit their parent's scope
        var isTagsOrLocks = pageType == typeof(TagsPage) || pageType == typeof(LocksPage);
        var isAppGwSection = pageType == typeof(AppGwSectionPage);
        var isAppGwSubDetail = pageType == typeof(AppGwBackendPoolDetailPage) || pageType == typeof(AppGwRoutingRuleDetailPage);
        var isAppGw = _activeNavContext?.Resource?.Type.Equals("Microsoft.Network/applicationGateways", StringComparison.OrdinalIgnoreCase) == true;

        if (_activeNavContext?.Resource is not null && isAppGw && (pageType == typeof(ResourceDetailPage) || isAppGwSection || isAppGwSubDetail || isTagsOrLocks))
        {
            // AppGW-specific nav
            AddNavItems(
                CreateSvgNavItem("Overview", "AppGwOverview", "applicationGateways.svg"),
                CreateSvgNavItem("Backend Pools", "AppGwBackendPools", "backendPools.svg"),
                CreateSvgNavItem("Backend Settings", "AppGwBackendSettings", "backendSettings.svg"),
                CreateSvgNavItem("Frontend IP", "AppGwFrontendIP", "frontendIP.svg"),
                CreateSvgNavItem("Private Link", "AppGwPrivateLink", "privateLink.svg"),
                CreateSvgNavItem("SSL Settings", "AppGwSsl", "sslSettings.svg"),
                CreateSvgNavItem("Listeners", "AppGwListeners", "listeners.svg"),
                CreateSvgNavItem("Routing Rules", "AppGwRoutingRules", "routingRules.svg"),
                CreateSvgNavItem("Rewrite Sets", "AppGwRewriteSets", "rewriteSets.svg"),
                CreateSvgNavItem("Health Probes", "AppGwHealthProbes", "healthProbes.svg"),
                CreateSvgNavItem("Configuration", "AppGwConfig", "configuration.svg"),
                CreateSvgNavItem("WAF", "AppGwWaf", "waf.svg"),
                CreateSvgNavItem("JWT Validation", "AppGwJwt", "jwtValidation.svg"),
                CreateSvgNavItem("Manage Tags", "ResourceTags", "tags.svg"),
                CreateSvgNavItem("Manage Locks", "ResourceLocks", "locks.svg"));
        }
        else if (_activeNavContext?.Resource is not null && (pageType == typeof(ResourceDetailPage) || isTagsOrLocks))
        {
            var iconFile = Helpers.ResourceIconResolver.GetIconFileName(_activeNavContext.Resource.Type);
            AddNavItems(
                CreateSvgNavItem("Overview", "ResourceDetail", iconFile),
                CreateSvgNavItem("Manage Tags", "ResourceTags", "tags.svg"),
                CreateSvgNavItem("Manage Locks", "ResourceLocks", "locks.svg"));
        }
        else if (_activeNavContext?.ResourceGroupName is not null &&
                 (pageType == typeof(ResourceGroupDetailPage) || pageType == typeof(ResourcesPage) || pageType == typeof(ResourceDetailPage) || isAppGwSection || isTagsOrLocks))
        {
            AddNavItems(
                CreateSvgNavItem("Overview", "RGDetail", "resourceGroups.svg"),
                CreateSvgNavItem("Manage Tags", "RGTags", "tags.svg"),
                CreateSvgNavItem("Manage Locks", "RGLocks", "locks.svg"));
        }
        else
        {
            AddNavItems(
                CreateSvgNavItem("Overview", "SubscriptionDetail", "subscriptions.svg"),
                CreateSvgNavItem("Manage Tags", "ManageTags", "tags.svg"),
                CreateSvgNavItem("Manage Locks", "ManageLocks", "locks.svg"),
                CreateSvgNavItem("Preview Features", "PreviewFeatures", "previewFeatures.svg"));
        }

        var activeTag = pageType switch
        {
            var t when t == typeof(SubscriptionDetailPage) => "SubscriptionDetail",
            var t when t == typeof(FeaturesPage) || t == typeof(FeatureDetailPage) => "PreviewFeatures",
            var t when t == typeof(ResourceGroupDetailPage) => "RGDetail",
            var t when t == typeof(ResourceDetailPage) => isAppGw ? "AppGwOverview" : "ResourceDetail",
            var t when t == typeof(TagsPage) => "ManageTags",
            var t when t == typeof(LocksPage) => "ManageLocks",
            var t when t == typeof(AppGwSectionPage) => _lastAppGwNavTag,
            var t when t == typeof(AppGwBackendPoolDetailPage) => "AppGwBackendPools",
            var t when t == typeof(AppGwRoutingRuleDetailPage) => "AppGwRoutingRules",
            _ => null,
        };

        NavView.SelectedItem = activeTag is not null
            ? _contextNavItems.FirstOrDefault(i => i.Tag?.ToString() == activeTag
                || (activeTag == "ManageTags" && (i.Tag?.ToString() == "RGTags" || i.Tag?.ToString() == "ResourceTags"))
                || (activeTag == "ManageLocks" && (i.Tag?.ToString() == "RGLocks" || i.Tag?.ToString() == "ResourceLocks")))
            : null;
    }

    private void AddNavItems(params NavigationViewItem[] items)
    {
        _contextNavItems.AddRange(items);
        foreach (var item in items)
        {
            NavView.MenuItems.Add(item);
        }
    }

    private static NavigationViewItem CreateSvgNavItem(string content, string tag, string iconFileName)
    {
        return new NavigationViewItem
        {
            Content = content,
            Tag = tag,
            Icon = new ImageIcon
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri($"ms-appx:///Assets/Icons/{iconFileName}")),
                Width = 20,
                Height = 20,
            },
        };
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        _activeSubscription = null;
        _activeNavContext = null;
        ContentFrame.Navigate(typeof(HomePage));
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

        // Resource-level nav
        if (_activeNavContext?.Resource is not null)
        {
            switch (tag)
            {
                case "ResourceDetail":
                case "AppGwOverview":
                    ContentFrame.Navigate(typeof(ResourceDetailPage), _activeNavContext);
                    return;
                case "ResourceTags":
                    NavigateToTags(_activeNavContext.Resource.ResourceId, BuildBreadcrumbs("resource"));
                    return;
                case "ResourceLocks":
                    NavigateToLocks(_activeNavContext.Resource.ResourceId, BuildBreadcrumbs("resource"));
                    return;
            }

            // AppGW section nav
            if (tag?.StartsWith("AppGw") == true)
            {
                var section = tag switch
                {
                    "AppGwBackendPools" => AppGwSection.BackendPools,
                    "AppGwBackendSettings" => AppGwSection.BackendSettings,
                    "AppGwFrontendIP" => AppGwSection.FrontendIP,
                    "AppGwPrivateLink" => AppGwSection.PrivateLink,
                    "AppGwSsl" => AppGwSection.SslSettings,
                    "AppGwListeners" => AppGwSection.Listeners,
                    "AppGwRoutingRules" => AppGwSection.RoutingRules,
                    "AppGwRewriteSets" => AppGwSection.RewriteSets,
                    "AppGwHealthProbes" => AppGwSection.HealthProbes,
                    "AppGwConfig" => AppGwSection.Configuration,
                    "AppGwWaf" => AppGwSection.Waf,
                    "AppGwJwt" => AppGwSection.JwtValidation,
                    _ => AppGwSection.Overview,
                };
                ContentFrame.Navigate(typeof(AppGwSectionPage), (_activeNavContext, section));
                return;
            }
        }

        // RG-level nav
        if (_activeNavContext?.ResourceGroupName is not null)
        {
            var rgId = $"/subscriptions/{_activeNavContext.SubscriptionId}/resourceGroups/{_activeNavContext.ResourceGroupName}";
            switch (tag)
            {
                case "RGDetail":
                    ContentFrame.Navigate(typeof(ResourceGroupDetailPage), _activeNavContext with { Resource = null });
                    return;
                case "RGTags":
                    NavigateToTags(rgId, BuildBreadcrumbs("rg"));
                    return;
                case "RGLocks":
                    NavigateToLocks(rgId, BuildBreadcrumbs("rg"));
                    return;
            }
        }

        // Subscription-level nav
        if (_activeSubscription is not null)
        {
            switch (tag)
            {
                case "SubscriptionDetail":
                    ContentFrame.Navigate(typeof(SubscriptionDetailPage), _activeSubscription);
                    return;
                case "ManageTags":
                    NavigateToTags($"/subscriptions/{_activeSubscription.Id}", BuildBreadcrumbs("sub"));
                    return;
                case "ManageLocks":
                    NavigateToLocks($"/subscriptions/{_activeSubscription.Id}", BuildBreadcrumbs("sub"));
                    return;
                case "PreviewFeatures":
                    ContentFrame.Navigate(typeof(FeaturesPage), _activeSubscription);
                    return;
            }
        }
    }

    private void NavigateToTags(string resourceId, List<string> breadcrumbs)
    {
        var navParam = (_activeNavContext as object) ?? _activeSubscription;
        ContentFrame.Navigate(typeof(TagsPage), (resourceId, breadcrumbs, navParam));
    }

    private void NavigateToLocks(string resourceId, List<string> breadcrumbs)
    {
        var navParam = (_activeNavContext as object) ?? _activeSubscription;
        ContentFrame.Navigate(typeof(LocksPage), (resourceId, breadcrumbs, navParam));
    }

    private List<string> BuildBreadcrumbs(string scope)
    {
        var crumbs = new List<string>();

        if (_activeSubscription is not null)
        {
            crumbs.Add("Subscriptions");
            crumbs.Add("Subscription");
        }

        if (scope == "sub")
        {
            return crumbs;
        }

        if (_activeNavContext?.ResourceGroupName is not null)
        {
            crumbs.Add("Resource Group");
        }

        if (scope == "rg")
        {
            return crumbs;
        }

        if (_activeNavContext?.Resource is not null)
        {
            crumbs.Add(_activeNavContext.Resource.SingularType);
        }

        return crumbs;
    }
}
