using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceGroupsPage : Page
{
    public ResourceGroupsViewModel ViewModel { get; }

    private BreadcrumbHelper? _breadcrumbHelper;
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
            _breadcrumbHelper = new BreadcrumbHelper(Breadcrumb, EllipsisButton);
            _breadcrumbHelper.Add("Subscriptions", () => { Frame.BackStack.Clear(); Frame.Navigate(typeof(SubscriptionsPage)); });
            _breadcrumbHelper.Add("Subscription", () => Frame.Navigate(typeof(SubscriptionDetailPage), _navCtx!.Subscription));
            _breadcrumbHelper.Add("Resource Groups", () => { });
            _breadcrumbHelper.Apply();

            var subItem = new SubscriptionItem(ctx.SubscriptionId, ctx.SubscriptionName, "", "");
            await ViewModel.LoadForSubscriptionAsync(subItem, default);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        _breadcrumbHelper?.HandleClick(args.Index);
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

    private void LocationFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressFilterEvents) return;

        ViewModel.SelectedLocations.Clear();
        for (var i = 0; i < LocationFilterList.ItemsSourceView.Count; i++)
        {
            if (LocationFilterList.TryGetElement(i) is CheckBox cb && cb.IsChecked == true)
            {
                ViewModel.SelectedLocations.Add(cb.Content?.ToString() ?? "");
            }
        }

        ViewModel.OnFilterChanged();
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        _suppressFilterEvents = true;
        for (var i = 0; i < LocationFilterList.ItemsSourceView.Count; i++)
        {
            if (LocationFilterList.TryGetElement(i) is CheckBox cb)
            {
                cb.IsChecked = false;
            }
        }

        _suppressFilterEvents = false;
        ViewModel.ClearFilters();
    }
}
