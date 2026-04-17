using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using AzureDesktop.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Controls;

public sealed partial class BreadcrumbControl : UserControl
{
    private readonly List<(string Label, Action Navigate)> _fullChain = [];
    private readonly ObservableCollection<string> _visibleItems = [];

    public BreadcrumbControl()
    {
        InitializeComponent();
        Breadcrumb.ItemsSource = _visibleItems;
    }

    /// <summary>
    /// Builds the breadcrumb chain automatically from a NavigationContext.
    /// Call this from MainWindow after each navigation.
    /// </summary>
    public void Build(NavigationContext? ctx, Frame frame)
    {
        _fullChain.Clear();
        _visibleItems.Clear();
        EllipsisButton.Visibility = Visibility.Collapsed;

        if (ctx is null)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        Visibility = Visibility.Visible;

        var chain = ctx.BuildBreadcrumbChain();
        foreach (var (label, pageType, navCtx) in chain)
        {
            var capturedType = pageType;
            var capturedCtx = navCtx;
            _fullChain.Add((label, () => NavigateTo(frame, capturedType, capturedCtx)));
        }

        Apply();
    }

    /// <summary>
    /// Manual setup: clear, add items, apply. For pages that need custom chains.
    /// </summary>
    public void Clear()
    {
        _fullChain.Clear();
        _visibleItems.Clear();
        EllipsisButton.Visibility = Visibility.Collapsed;
    }

    public void Add(string label, Action navigate)
    {
        _fullChain.Add((label, navigate));
    }

    public void Apply()
    {
        _visibleItems.Clear();

        var maxVisible = 3;
        var collapsedCount = Math.Max(0, _fullChain.Count - maxVisible);

        if (collapsedCount > 0)
        {
            EllipsisButton.Visibility = Visibility.Visible;

            var flyout = new MenuFlyout();
            for (var i = 0; i < collapsedCount; i++)
            {
                var index = i;
                var item = new MenuFlyoutItem { Text = _fullChain[i].Label };
                item.Click += (_, _) => _fullChain[index].Navigate();
                flyout.Items.Add(item);
            }

            EllipsisButton.Flyout = flyout;

            for (var i = collapsedCount; i < _fullChain.Count; i++)
            {
                _visibleItems.Add(_fullChain[i].Label);
            }
        }
        else
        {
            EllipsisButton.Visibility = Visibility.Collapsed;

            foreach (var (label, _) in _fullChain)
            {
                _visibleItems.Add(label);
            }
        }
    }

    private static void NavigateTo(Frame frame, Type pageType, NavigationContext ctx)
    {
        if (pageType == typeof(SubscriptionsPage))
        {
            frame.BackStack.Clear();
            frame.Navigate(typeof(SubscriptionsPage));
        }
        else if (pageType == typeof(SubscriptionDetailPage))
        {
            frame.Navigate(typeof(SubscriptionDetailPage), ctx);
        }
        else if (pageType == typeof(ResourceGroupDetailPage))
        {
            frame.Navigate(typeof(ResourceGroupDetailPage), ctx);
        }
        else if (pageType == typeof(ResourceDetailPage))
        {
            frame.Navigate(typeof(ResourceDetailPage), ctx);
        }
        else if (pageType == typeof(FeaturesPage))
        {
            frame.Navigate(typeof(FeaturesPage), ctx);
        }
        else if (pageType == typeof(FeatureDetailPage))
        {
            frame.Navigate(typeof(FeatureDetailPage), ctx);
        }
        else if (pageType == typeof(ResourceProvidersPage))
        {
            frame.Navigate(typeof(ResourceProvidersPage), ctx);
        }
        else if (pageType == typeof(ResourceProviderDetailPage))
        {
            frame.Navigate(typeof(ResourceProviderDetailPage), ctx);
        }
        else if (pageType == typeof(AppGwSectionPage))
        {
            frame.Navigate(typeof(AppGwSectionPage), ctx);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        var collapsedCount = Math.Max(0, _fullChain.Count - 3);
        var actualIndex = collapsedCount + args.Index;

        if (actualIndex >= 0 && actualIndex < _fullChain.Count - 1)
        {
            _fullChain[actualIndex].Navigate();
        }
    }
}
