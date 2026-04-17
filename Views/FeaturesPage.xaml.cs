using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class FeaturesPage : Page
{
    private CancellationTokenSource? _cts;
    public FeaturesViewModel ViewModel { get; }
    private SubscriptionItem? _subItem;

    public FeaturesPage()
    {
        ViewModel = App.GetService<FeaturesViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (e.Parameter is NavigationContext ctx)
        {
            _subItem = ctx.Subscription;

            await ViewModel.LoadProvidersAsync(ctx.SubscriptionId, _cts.Token);
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

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
