using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class TagsPage : NavigablePage
{
    public override string PageLabel => "Tags";
    public override string? ActiveNavTag => "Tags";
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

    public TagManagerViewModel ViewModel { get; }

    public TagsPage()
    {
        ViewModel = App.GetService<TagManagerViewModel>();
        InitializeComponent();
    }

    protected override async void OnContextReady(NavigationContext? ctx)
    {
        if (ctx is not null)
        {
            var resourceId = GetResourceId(ctx);
            await ViewModel.LoadTagsAsync(resourceId, Cts!.Token);
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
        result.Add(new BreadcrumbEntry("Tags", null, null));
        return result.ToArray();
    }

    private void EditTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag }) tag.BeginEdit();
    }

    private void CancelEditTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag }) tag.CancelEdit();
    }

    private async void SaveEditTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag })
            await ViewModel.SaveEditCommand.ExecuteAsync(tag);
    }

    private void DeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag }) tag.BeginDelete();
    }

    private void CancelDeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag }) tag.CancelDelete();
    }

    private async void ConfirmDeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag })
            await ViewModel.ConfirmDeleteCommand.ExecuteAsync(tag);
    }
}
