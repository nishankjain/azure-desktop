using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class SubscriptionDetailPage : Page
{
    public SubscriptionDetailViewModel ViewModel { get; }
    public ResourceGroupsViewModel RgViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private SubscriptionItem? _item;
    private bool _suppressFilterEvents;

    public SubscriptionDetailPage()
    {
        ViewModel = App.GetService<SubscriptionDetailViewModel>();
        RgViewModel = App.GetService<ResourceGroupsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
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

            await RgViewModel.LoadForSubscriptionAsync(item, default);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index == 0)
        {
            Frame.BackStack.Clear();
            Frame.Navigate(typeof(SubscriptionsPage));
        }
    }

    private void ResourceGroupList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ResourceGroupItem rgItem && _item is not null)
        {
            var ctx = new NavigationContext(_item, rgItem.Name, rgItem.Location);
            Frame.Navigate(typeof(ResourceGroupDetailPage), ctx);
        }
    }

    private void LocationFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressFilterEvents)
        {
            return;
        }

        RgViewModel.SelectedLocations.Clear();
        for (var i = 0; i < LocationFilterList.ItemsSourceView.Count; i++)
        {
            if (LocationFilterList.TryGetElement(i) is CheckBox cb && cb.IsChecked == true)
            {
                RgViewModel.SelectedLocations.Add(cb.Content?.ToString() ?? "");
            }
        }

        RgViewModel.OnFilterChanged();
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
        RgViewModel.ClearFilters();
    }

    private void SortByName_Click(object sender, RoutedEventArgs e)
    {
        RgViewModel.ToggleSort("Name");
        var arrow = RgViewModel.SortAscending ? "↑" : "↓";
        SortNameButton.Content = $"Name {arrow}";
    }
}
