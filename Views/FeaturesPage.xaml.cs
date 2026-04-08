using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class FeaturesPage : Page
{
    public FeaturesViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private SubscriptionItem? _subItem;

    public FeaturesPage()
    {
        ViewModel = App.GetService<FeaturesViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is SubscriptionItem sub)
        {
            _subItem = sub;
            BreadcrumbItems.Clear();
            BreadcrumbItems.Add("Subscriptions");
            BreadcrumbItems.Add(sub.Name);
            BreadcrumbItems.Add("Preview Features");
            Breadcrumb.ItemsSource = BreadcrumbItems;

            await ViewModel.LoadProvidersAsync(sub.Id);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        switch (args.Index)
        {
            case 0:
                Frame.BackStack.Clear();
                Frame.Navigate(typeof(SubscriptionsPage));
                break;
            case 1 when _subItem is not null:
                Frame.Navigate(typeof(SubscriptionDetailPage), _subItem);
                break;
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
            Frame.Navigate(typeof(FeatureDetailPage), (entry, _subItem));
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
