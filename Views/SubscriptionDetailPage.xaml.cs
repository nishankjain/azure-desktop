using System.Collections.ObjectModel;
using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class SubscriptionDetailPage : NavigablePage
{
    public override string PageLabel => "Subscription";
    public override string? ActiveNavTag => "SubscriptionDetail";
    protected override bool IsOverviewPage => true;
    public override NavItemDefinition[] GetNavItems() => SubscriptionNavItems.Get();

    public SubscriptionDetailViewModel ViewModel { get; }

    public SubscriptionDetailPage()
    {
        ViewModel = App.GetService<SubscriptionDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnContextReady(NavigationContext? ctx)
    {
        if (ctx is not null)
            ViewModel.Subscription = ctx.Subscription;
    }
}
