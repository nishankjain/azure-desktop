using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourcesPage : Page
{
    public ResourcesViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private NavigationContext? _navCtx;
    private bool _suppressFilterEvents;

    public ResourcesPage()
    {
        ViewModel = App.GetService<ResourcesViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is NavigationContext ctx && ctx.ResourceGroupName is not null)
        {
            _navCtx = ctx;
            BreadcrumbItems.Clear();
            BreadcrumbItems.Add("Subscriptions");
            BreadcrumbItems.Add(ctx.SubscriptionName);
            BreadcrumbItems.Add(ctx.ResourceGroupName);
            BreadcrumbItems.Add("Resources");
            Breadcrumb.ItemsSource = BreadcrumbItems;

            await ViewModel.LoadForResourceGroupAsync(
                ctx.SubscriptionId, ctx.SubscriptionName, ctx.ResourceGroupName, default);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (_navCtx is null)
        {
            return;
        }

        switch (args.Index)
        {
            case 0:
                Frame.BackStack.Clear();
                Frame.Navigate(typeof(SubscriptionsPage));
                break;
            case 1:
                Frame.Navigate(typeof(SubscriptionDetailPage), _navCtx.Subscription);
                break;
            case 2:
                Frame.Navigate(typeof(ResourceGroupDetailPage), _navCtx);
                break;
        }
    }

    private void SortByName_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleSort("Name");
        UpdateSortButtons();
    }

    private void SortByType_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleSort("Type");
        UpdateSortButtons();
    }

    private void SortByLocation_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleSort("Location");
        UpdateSortButtons();
    }

    private void UpdateSortButtons()
    {
        var arrow = ViewModel.SortAscending ? "↑" : "↓";
        SortNameButton.Content = ViewModel.SortField == "Name" ? $"Name {arrow}" : "Name";
        SortTypeButton.Content = ViewModel.SortField == "Type" ? $"Type {arrow}" : "Type";
        SortLocationButton.Content = ViewModel.SortField == "Location" ? $"Location {arrow}" : "Location";
    }

    private void TypeFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressFilterEvents) return;
        SyncCheckboxFilter(TypeFilterList, ViewModel.SelectedTypes);
        ViewModel.OnFilterChanged();
    }

    private void LocationFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressFilterEvents) return;
        SyncCheckboxFilter(LocationFilterList, ViewModel.SelectedLocations);
        ViewModel.OnFilterChanged();
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        _suppressFilterEvents = true;
        ClearCheckboxes(TypeFilterList);
        ClearCheckboxes(LocationFilterList);
        _suppressFilterEvents = false;
        ViewModel.ClearFilters();
    }

    private static void SyncCheckboxFilter(Microsoft.UI.Xaml.Controls.ItemsRepeater repeater, HashSet<string> target)
    {
        target.Clear();
        for (var i = 0; i < repeater.ItemsSourceView.Count; i++)
        {
            if (repeater.TryGetElement(i) is CheckBox cb && cb.IsChecked == true)
            {
                target.Add(cb.Content?.ToString() ?? "");
            }
        }
    }

    private static void ClearCheckboxes(Microsoft.UI.Xaml.Controls.ItemsRepeater repeater)
    {
        for (var i = 0; i < repeater.ItemsSourceView.Count; i++)
        {
            if (repeater.TryGetElement(i) is CheckBox cb)
            {
                cb.IsChecked = false;
            }
        }
    }

    private void Resource_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border { Tag: ResourceItem item } && _navCtx is not null)
        {
            var ctx = _navCtx with { Resource = item };
            Frame.Navigate(typeof(ResourceDetailPage), ctx);
        }
    }
}
