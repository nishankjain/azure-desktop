using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

/// <summary>
/// Base class for all navigable pages. Handles context storage, cancellation tokens,
/// and automatic breadcrumb generation from the navigation hierarchy.
/// </summary>
public abstract class NavigablePage : Page, INavigablePage
{
    protected NavigationContext? NavCtx { get; private set; }
    protected CancellationTokenSource? Cts { get; private set; }

    /// <summary>Display label for this page in breadcrumbs (e.g. "Backend Pools").</summary>
    public abstract string PageLabel { get; }

    /// <summary>Nav sidebar items to show when this page is active.</summary>
    public abstract NavItemDefinition[] GetNavItems();

    /// <summary>Tag of the active nav item for highlighting.</summary>
    public abstract string? ActiveNavTag { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Cts?.Cancel();
        Cts = new CancellationTokenSource();
        if (e.Parameter is NavigationContext ctx)
            NavCtx = ctx;
        OnContextReady(NavCtx);
    }

    /// <summary>
    /// Called after NavCtx is set. Override to load data, set up bindings, etc.
    /// </summary>
    protected virtual void OnContextReady(NavigationContext? ctx) { }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Cts?.Cancel();
        Cts?.Dispose();
        Cts = null;
        base.OnNavigatedFrom(e);
    }

    /// <summary>
    /// If true, this page is the overview/root page of its hierarchy level.
    /// The breadcrumb won't add a duplicate segment for PageLabel.
    /// </summary>
    protected virtual bool IsOverviewPage => false;

    /// <summary>
    /// Builds breadcrumbs automatically from the NavigationContext hierarchy.
    /// Override for custom breadcrumb chains.
    /// </summary>
    public virtual BreadcrumbEntry[] GetBreadcrumbs()
    {
        var ctx = NavCtx;
        if (ctx is null) return [new(PageLabel, null, null)];

        var chain = new List<BreadcrumbEntry>();
        var subCtx = ctx with { ResourceGroupName = null, ResourceGroupLocation = null, Resource = null, Section = null, DetailItemName = null, Feature = null, ResourceProvider = null, PageLabel = null };

        chain.Add(new("Subscriptions", typeof(SubscriptionsPage), subCtx));

        if (ctx.ResourceGroupName is null)
        {
            // Subscription level — "Subscription" is the last hierarchy segment
            if (!IsOverviewPage)
            {
                chain.Add(new("Subscription", typeof(SubscriptionDetailPage), subCtx));
                chain.Add(new(PageLabel, null, null));
            }
            else
            {
                chain.Add(new("Subscription", null, null));
            }
            return chain.ToArray();
        }

        chain.Add(new("Subscription", typeof(SubscriptionDetailPage), subCtx));

        var rgCtx = ctx with { Resource = null, Section = null, DetailItemName = null, Feature = null, ResourceProvider = null, PageLabel = null };

        if (ctx.Resource is null)
        {
            chain.Add(new("Resource Groups", typeof(ResourceGroupsPage), rgCtx));
            if (!IsOverviewPage)
            {
                chain.Add(new("Resource Group", typeof(ResourceGroupDetailPage), rgCtx));
                chain.Add(new(PageLabel, null, null));
            }
            else
            {
                chain.Add(new("Resource Group", null, null));
            }
            return chain.ToArray();
        }

        chain.Add(new("Resource Groups", typeof(ResourceGroupsPage), rgCtx));
        chain.Add(new("Resource Group", typeof(ResourceGroupDetailPage), rgCtx));

        var resCtx = ctx with { Section = null, DetailItemName = null, Feature = null, ResourceProvider = null, PageLabel = null };
        var resLabel = Helpers.ResourceTypeLabels.GetLabel(ctx.Resource.Type);

        if (!IsOverviewPage)
        {
            chain.Add(new(resLabel, typeof(ResourceDetailPage), resCtx));
            chain.Add(new(PageLabel, null, null));
        }
        else
        {
            chain.Add(new(resLabel, null, null));
        }

        return chain.ToArray();
    }
}

/// <summary>
/// Base class for AppGw section pages. Provides shared nav items,
/// breadcrumbs with the gateway in the chain, and ViewModel access.
/// </summary>
public abstract class AppGwPageBase : NavigablePage
{
    public AppGwViewModel ViewModel { get; }

    protected AppGwPageBase()
    {
        ViewModel = App.GetService<AppGwViewModel>();
    }

    public override NavItemDefinition[] GetNavItems() => Helpers.AppGwNavItems.Get();

    protected override async void OnContextReady(NavigationContext? ctx)
    {
        if (ctx?.Resource is not null)
            await ViewModel.LoadAsync(ctx.Resource.ResourceId, Cts!.Token);
        OnDataLoaded();
    }

    /// <summary>Called after ViewModel data is loaded. Override to populate UI.</summary>
    protected virtual void OnDataLoaded() { }
}
