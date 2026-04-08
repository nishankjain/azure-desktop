using System.Collections.ObjectModel;
using AzureDesktop.Controls;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class SubscriptionDetailPage : Page
{
    public SubscriptionDetailViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private SubscriptionItem? _item;

    public SubscriptionDetailPage()
    {
        ViewModel = App.GetService<SubscriptionDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is SubscriptionItem item)
        {
            _item = item;
            ViewModel.Subscription = item;
            BreadcrumbItems.Clear();
            BreadcrumbItems.Add("Subscriptions");
            BreadcrumbItems.Add(item.Name);
            Breadcrumb.ItemsSource = BreadcrumbItems;
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index == 0)
        {
            // Go back to subscriptions
            Frame.BackStack.Clear();
            Frame.Navigate(typeof(SubscriptionsPage));
        }
    }

    private void ViewResourceGroups_Click(object sender, RoutedEventArgs e)
    {
        if (_item is not null)
        {
            var ctx = new NavigationContext(_item);
            Frame.Navigate(typeof(ResourceGroupsPage), ctx);
        }
    }

    private async void ManageTags_Click(object sender, RoutedEventArgs e)
    {
        if (_item is not null)
        {
            await TagManagerDialog.ShowAsync($"/subscriptions/{_item.Id}", XamlRoot);
        }
    }

    private async void ManageLocks_Click(object sender, RoutedEventArgs e)
    {
        if (_item is not null)
        {
            await LockManagerDialog.ShowAsync($"/subscriptions/{_item.Id}", XamlRoot);
        }
    }

    private void ViewFeatures_Click(object sender, RoutedEventArgs e)
    {
        if (_item is not null)
        {
            Frame.Navigate(typeof(FeaturesPage), _item);
        }
    }

    private void ViewResourceProviders_Click(object sender, RoutedEventArgs e)
    {
        if (_item is not null)
        {
            Frame.Navigate(typeof(ResourceProvidersPage), _item);
        }
    }
}
