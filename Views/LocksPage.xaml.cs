using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class LocksPage : NavigablePage
{
    public override string PageLabel => "Locks";
    public override string? ActiveNavTag => "Locks";
    public override NavItemDefinition[] GetNavItems()
    {
        if (NavCtx?.Resource?.Type.Equals("Microsoft.Network/applicationGateways", StringComparison.OrdinalIgnoreCase) == true)
            return AppGwNavItems.Get();
        if (NavCtx?.Resource is not null)
            return ResourceNavItems.Get(NavCtx.Resource.Type);
        if (NavCtx?.ResourceGroupName is not null)
            return ResourceGroupNavItems.Get();
        return SubscriptionNavItems.Get();
    }

    public LockManagerViewModel ViewModel { get; }

    public LocksPage()
    {
        ViewModel = App.GetService<LockManagerViewModel>();
        InitializeComponent();
    }

    protected override async void OnContextReady(NavigationContext? ctx)
    {
        if (ctx is not null)
        {
            var resourceId = GetResourceId(ctx);
            await ViewModel.LoadLocksAsync(resourceId, Cts!.Token);
        }
    }

    private static string GetResourceId(NavigationContext ctx)
    {
        if (ctx.Resource is not null)
            return ctx.Resource.ResourceId;
        if (ctx.ResourceGroupName is not null)
            return $"/subscriptions/{ctx.SubscriptionId}/resourceGroups/{ctx.ResourceGroupName}";
        return $"/subscriptions/{ctx.SubscriptionId}";
    }

    public override BreadcrumbEntry[] GetBreadcrumbs()
    {
        var ctx = NavCtx!;
        var chain = ctx.BuildBreadcrumbChain();
        var result = new List<BreadcrumbEntry>();
        foreach (var (label, pageType, navCtx) in chain)
            result.Add(new BreadcrumbEntry(label, pageType, navCtx));
        result.Add(new BreadcrumbEntry("Locks", null, null));
        return result.ToArray();
    }

    private void EditLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry }) entry.BeginEdit();
    }

    private void CancelEditLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry }) entry.CancelEdit();
    }

    private async void SaveEditLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry })
            await ViewModel.SaveEditCommand.ExecuteAsync(entry);
    }

    private void DeleteLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry }) entry.BeginDelete();
    }

    private void CancelDeleteLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry }) entry.CancelDelete();
    }

    private async void ConfirmDeleteLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry })
            await ViewModel.ConfirmDeleteCommand.ExecuteAsync(entry);
    }
}
