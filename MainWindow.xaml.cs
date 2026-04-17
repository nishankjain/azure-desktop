using AzureDesktop.Services;
using AzureDesktop.ViewModels;
using AzureDesktop.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace AzureDesktop;

public sealed partial class MainWindow : Window
{
    private const double CompactThreshold = 640;

    private SubscriptionItem? _activeSubscription;
    private NavigationContext? _activeNavContext;
    private readonly List<NavigationViewItem> _contextNavItems = [];

    public OperationManager OperationMgr { get; } = App.GetService<OperationManager>();

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets", "AppIcon.ico"));

        // Configure caption buttons to match title bar
        var titleBar = AppWindow.TitleBar;
        AppTitleBar.Padding = new Microsoft.UI.Xaml.Thickness(0, 0, titleBar.RightInset, 0);

        ContentFrame.Navigated += ContentFrame_Navigated;
        NavView.DisplayModeChanged += NavView_DisplayModeChanged;
        SizeChanged += MainWindow_SizeChanged;

        // Update badge when operations change
        OperationMgr.Operations.CollectionChanged += (_, _) => DispatcherQueue.TryEnqueue(UpdateBadge);
        OperationMgr.OperationUpdated += () => DispatcherQueue.TryEnqueue(UpdateBadge);

        OperationsList.ItemsSource = OperationMgr.Operations;

        ContentFrame.Navigate(typeof(HomePage));
    }

    private OperationEntry? _lastTrackedOperation;
    private Microsoft.UI.Xaml.DispatcherTimer? _statusHideTimer;

    private void UpdateBadge()
    {
        var unread = OperationMgr.UnreadCount;
        if (unread > 0)
        {
            OperationBadge.Value = unread;
            OperationBadge.Visibility = Visibility.Visible;
        }
        else
        {
            OperationBadge.Visibility = Visibility.Collapsed;
        }

        NoOperationsText.Visibility = OperationMgr.Operations.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        UpdateInlineStatus();
    }

    private void UpdateInlineStatus()
    {
        var activeCount = OperationMgr.ActiveCount;
        var active = OperationMgr.LatestActive;

        if (active is not null)
        {
            _lastTrackedOperation = active;
            _statusHideTimer?.Stop();
            InlineSpinner.Visibility = Visibility.Visible;
            InlineResultIcon.Visibility = Visibility.Collapsed;

            if (activeCount > 1)
            {
                InlineStatusText.Text = $"{active.InProgressText} {active.ResourceName} (+{activeCount - 1} more)";
            }
            else
            {
                InlineStatusText.Text = $"{active.InProgressText} {active.ResourceName}";
            }

            InlineStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
            InlineStatusBorder.Visibility = Visibility.Visible;
        }
        else if (_lastTrackedOperation is not null && _lastTrackedOperation.Status != Services.OperationStatus.InProgress)
        {
            InlineSpinner.Visibility = Visibility.Collapsed;
            InlineResultIcon.Visibility = Visibility.Visible;

            if (_lastTrackedOperation.Status == Services.OperationStatus.Succeeded)
            {
                InlineResultIcon.Glyph = "\uE73E";
                InlineResultIcon.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                InlineStatusText.Text = $"{_lastTrackedOperation.CompletedText} {_lastTrackedOperation.ResourceName}";
                InlineStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
            }
            else
            {
                InlineResultIcon.Glyph = "\uEA39";
                InlineResultIcon.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                InlineStatusText.Text = $"Failed to {_lastTrackedOperation.OperationName.ToLower()} {_lastTrackedOperation.ResourceName}";
                InlineStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
            }

            InlineStatusBorder.Visibility = Visibility.Visible;

            _statusHideTimer?.Stop();
            _statusHideTimer = new Microsoft.UI.Xaml.DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _statusHideTimer.Tick += (_, _) =>
            {
                _statusHideTimer.Stop();
                InlineStatusBorder.Visibility = Visibility.Collapsed;
                _lastTrackedOperation = null;
            };
            _statusHideTimer.Start();
        }
    }

    private void NotificationFlyout_Opened(object sender, object e)
    {
        OperationMgr.MarkAllRead();
        UpdateBadge();
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
        }
        else
        {
            NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
        }
    }

    private void NavView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
    }

    private string? _lastAppGwNavTag;

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        BackButton.Visibility = ContentFrame.CanGoBack
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Hide home button when already on home page
        HomeButton.Visibility = e.SourcePageType == typeof(HomePage)
            ? Visibility.Collapsed
            : Visibility.Visible;

        // Trim back stack to prevent unbounded memory growth
        while (ContentFrame.BackStack.Count > 5)
        {
            ContentFrame.BackStack.RemoveAt(0);
        }

        _lastAppGwNavTag = null;

        if (e.Parameter is NavigationContext ctx)
        {
            _activeSubscription = ctx.Subscription;
            _activeNavContext = ctx;

            if (ctx.Section is not null)
            {
                _lastAppGwNavTag = ctx.Section switch
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

            BreadcrumbNav.Build(ctx, ContentFrame);
        }
        else if (e.SourcePageType == typeof(HomePage) || e.SourcePageType == typeof(SubscriptionsPage))
        {
            _activeSubscription = null;
            _activeNavContext = null;
            BreadcrumbNav.Build(null, ContentFrame);
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
                 (pageType == typeof(ResourceGroupDetailPage) || pageType == typeof(ResourceDetailPage) || isAppGwSection || isTagsOrLocks))
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
                Width = 16,
                Height = 16,
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

        // Operations flyout (not a page navigation)
        if (tag == "Operations")
        {
            FlyoutBase.ShowAttachedFlyout(NotificationBell);
            return;
        }

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
                    ContentFrame.Navigate(typeof(TagsPage), _activeNavContext with { PageLabel = "Tags" });
                    return;
                case "ResourceLocks":
                    ContentFrame.Navigate(typeof(LocksPage), _activeNavContext with { PageLabel = "Locks" });
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
                ContentFrame.Navigate(typeof(AppGwSectionPage), _activeNavContext with { Section = section, DetailItemName = null, PageLabel = null });
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
                    ContentFrame.Navigate(typeof(TagsPage), _activeNavContext with { Resource = null, PageLabel = "Tags" });
                    return;
                case "RGLocks":
                    ContentFrame.Navigate(typeof(LocksPage), _activeNavContext with { Resource = null, PageLabel = "Locks" });
                    return;
            }
        }

        // Subscription-level nav
        if (_activeSubscription is not null)
        {
            var subCtx = _activeNavContext ?? new NavigationContext(_activeSubscription);
            switch (tag)
            {
                case "SubscriptionDetail":
                    ContentFrame.Navigate(typeof(SubscriptionDetailPage), new NavigationContext(_activeSubscription));
                    return;
                case "ManageTags":
                    ContentFrame.Navigate(typeof(TagsPage), new NavigationContext(_activeSubscription, PageLabel: "Tags"));
                    return;
                case "ManageLocks":
                    ContentFrame.Navigate(typeof(LocksPage), new NavigationContext(_activeSubscription, PageLabel: "Locks"));
                    return;
                case "PreviewFeatures":
                    ContentFrame.Navigate(typeof(FeaturesPage), new NavigationContext(_activeSubscription, PageLabel: "Preview Features"));
                    return;
            }
        }
    }
}
