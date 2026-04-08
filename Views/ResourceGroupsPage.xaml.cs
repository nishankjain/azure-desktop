using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceGroupsPage : Page
{
    public ResourceGroupsViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private NavigationContext? _navCtx;
    private bool _suppressFilterEvents;

    public ResourceGroupsPage()
    {
        ViewModel = App.GetService<ResourceGroupsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is NavigationContext ctx)
        {
            _navCtx = ctx;
            BreadcrumbItems.Clear();
            BreadcrumbItems.Add("Subscriptions");
            BreadcrumbItems.Add(ctx.SubscriptionName);
            BreadcrumbItems.Add("Resource Groups");
            Breadcrumb.ItemsSource = BreadcrumbItems;

            var subItem = new SubscriptionItem(ctx.SubscriptionId, ctx.SubscriptionName, "", "");
            await ViewModel.LoadForSubscriptionAsync(subItem, default);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index == 0)
        {
            Frame.BackStack.Clear();
            Frame.Navigate(typeof(SubscriptionsPage));
        }
        else if (args.Index == 1 && _navCtx is not null)
        {
            Frame.Navigate(typeof(SubscriptionDetailPage), _navCtx.Subscription);
        }
    }

    private void ResourceGroupList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ResourceGroupItem rgItem && _navCtx is not null)
        {
            var ctx = _navCtx with { ResourceGroupName = rgItem.Name, ResourceGroupLocation = rgItem.Location };
            Frame.Navigate(typeof(ResourceGroupDetailPage), ctx);
        }
    }

    private void SortByName_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleSort("Name");
        var arrow = ViewModel.SortAscending ? "↑" : "↓";
        SortNameButton.Content = $"Name {arrow}";
    }

    private void LocationFilterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressFilterEvents)
        {
            return;
        }

        ViewModel.SelectedLocations.Clear();
        foreach (string item in LocationFilterList.SelectedItems)
        {
            ViewModel.SelectedLocations.Add(item);
        }

        ViewModel.OnFilterChanged();
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        _suppressFilterEvents = true;
        LocationFilterList.SelectedItems.Clear();
        _suppressFilterEvents = false;
        ViewModel.ClearFilters();
    }
}
