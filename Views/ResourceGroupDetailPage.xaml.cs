using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class ResourceGroupDetailPage : NavigablePage
{
    public override string PageLabel => "Resource Group";
    public override string? ActiveNavTag => "RGDetail";
    protected override bool IsOverviewPage => true;
    public override NavItemDefinition[] GetNavItems() => ResourceGroupNavItems.Get();

    public ResourceGroupDetailViewModel ViewModel { get; }

    public ResourceGroupDetailPage()
    {
        ViewModel = App.GetService<ResourceGroupDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnContextReady(NavigationContext? ctx)
    {
        if (ctx?.ResourceGroupName is not null)
        {
            var rgItem = new ResourceGroupItem(ctx.ResourceGroupName, ctx.ResourceGroupLocation ?? "");
            ViewModel.Load(ctx.SubscriptionId, rgItem);
        }
    }
}
