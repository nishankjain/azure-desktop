using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class FeaturesPage : NavigablePage
{
    public override string PageLabel => "Preview Features";
    public override string? ActiveNavTag => "PreviewFeatures";
    public override NavItemDefinition[] GetNavItems() => SubscriptionNavItems.Get();

    public FeaturesViewModel ViewModel { get; }
    private SubscriptionItem? _subItem;

    public FeaturesPage()
    {
        ViewModel = App.GetService<FeaturesViewModel>();
        InitializeComponent();
    }

    protected override async void OnContextReady(NavigationContext? ctx)
    {
        if (ctx is not null)
        {
            _subItem = ctx.Subscription;
            await ViewModel.LoadProvidersAsync(ctx.SubscriptionId, Cts!.Token);
        }
    }

    private async void Provider_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is string providerNamespace)
        {
            await ViewModel.SelectProviderAsync(providerNamespace);
        }
    }

    private void FeatureList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is FeatureEntry entry && _subItem is not null)
        {
            Frame.Navigate(typeof(FeatureDetailPage), new NavigationContext(_subItem, Feature: entry));
        }
    }

    private void SearchResult_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SearchResult is not null && _subItem is not null)
        {
            Frame.Navigate(typeof(FeatureDetailPage), (ViewModel.SearchResult, _subItem));
        }
    }
}
