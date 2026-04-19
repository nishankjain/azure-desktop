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
    private ILoadable? _currentLoadable;

    public OperationManager OperationMgr { get; } = App.GetService<OperationManager>();

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets", "AppIcon.ico"));

        // Configure caption buttons to match title bar
        var titleBar = AppWindow.TitleBar;
        titleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
        titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF);
        titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF);
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

        HomeButton.Visibility = e.SourcePageType == typeof(HomePage)
            ? Visibility.Collapsed
            : Visibility.Visible;

        while (ContentFrame.BackStack.Count > 5)
        {
            ContentFrame.BackStack.RemoveAt(0);
        }

        if (e.Parameter is NavigationContext ctx)
        {
            _activeSubscription = ctx.Subscription;
            _activeNavContext = ctx;
        }
        else if (e.SourcePageType == typeof(HomePage) || e.SourcePageType == typeof(SubscriptionsPage))
        {
            _activeSubscription = null;
            _activeNavContext = null;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            // Build breadcrumbs and nav items after page's OnNavigatedTo has run
            if (ContentFrame.Content is INavigablePage navigable)
                BreadcrumbNav.BuildFromPage(navigable, ContentFrame);
            else
                BreadcrumbNav.Build(null, ContentFrame);

            UpdateContextNav(e.SourcePageType);
        });
        BindPageLoader(ContentFrame.Content);
    }

    private void BindPageLoader(object? page)
    {
        // Unsubscribe from previous page's ViewModel
        if (_currentLoadable is System.ComponentModel.INotifyPropertyChanged oldNotify)
        {
            oldNotify.PropertyChanged -= Loadable_PropertyChanged;
        }

        _currentLoadable = null;
        PageLoader.Visibility = Visibility.Collapsed;
        PageLoader.IsActive = false;

        // Find the ViewModel property on the page
        var vmProp = page?.GetType().GetProperty("ViewModel");
        if (vmProp?.GetValue(page) is ILoadable loadable)
        {
            _currentLoadable = loadable;
            UpdatePageLoader(loadable.IsLoading);

            if (loadable is System.ComponentModel.INotifyPropertyChanged notify)
            {
                notify.PropertyChanged += Loadable_PropertyChanged;
            }
        }
    }

    private void Loadable_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILoadable.IsLoading) && _currentLoadable is not null)
        {
            DispatcherQueue.TryEnqueue(() => UpdatePageLoader(_currentLoadable.IsLoading));
        }
    }

    private void UpdatePageLoader(bool isLoading)
    {
        PageLoader.IsActive = isLoading;
        PageLoader.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateContextNav(Type pageType)
    {
        foreach (var item in _contextNavItems)
        {
            NavView.MenuItems.Remove(item);
        }
        _contextNavItems.Clear();

        if (ContentFrame.Content is INavigablePage navigable)
        {
            foreach (var def in navigable.GetNavItems())
            {
                AddNavItems(CreateSvgNavItem(def.Label, def.Tag, def.IconFile));
            }

            var activeTag = navigable.ActiveNavTag;
            NavView.SelectedItem = activeTag is not null
                ? _contextNavItems.FirstOrDefault(i => i.Tag?.ToString() == activeTag)
                : null;
        }
        else
        {
            NavView.SelectedItem = null;
        }
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
            return;

        var tag = item.Tag?.ToString();

        // Operations flyout (not a page navigation)
        if (tag == "Operations")
        {
            FlyoutBase.ShowAttachedFlyout(NotificationBell);
            return;
        }

        // Find the matching NavItemDefinition from the current page
        if (ContentFrame.Content is INavigablePage navigable)
        {
            var def = navigable.GetNavItems().FirstOrDefault(d => d.Tag == tag);
            if (def is not null)
            {
                ContentFrame.Navigate(def.PageType, _activeNavContext);
                return;
            }
        }
    }
}
