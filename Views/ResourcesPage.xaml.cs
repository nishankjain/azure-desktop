using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourcesPage : Page
{
    private CancellationTokenSource? _cts;
    public ResourcesViewModel ViewModel { get; }

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

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (e.Parameter is NavigationContext ctx && ctx.ResourceGroupName is not null)
        {
            _navCtx = ctx;

            await ViewModel.LoadForResourceGroupAsync(
                ctx.SubscriptionId, ctx.SubscriptionName, ctx.ResourceGroupName, _cts.Token);

            GroupedResourcesSource.Source = ViewModel.GroupedResources;
        }
    }

    private void Resource_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ResourceItem item && _navCtx is not null)
        {
            Frame.Navigate(typeof(ResourceDetailPage), _navCtx with { Resource = item });
        }
    }

    private void TypeFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressFilterEvents) return;
        SyncCheckboxFilter(TypeFilterList, ViewModel.SelectedTypes);
        ViewModel.OnFilterChanged();
        GroupedResourcesSource.Source = ViewModel.GroupedResources;
    }

    private void LocationFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressFilterEvents) return;
        SyncCheckboxFilter(LocationFilterList, ViewModel.SelectedLocations);
        ViewModel.OnFilterChanged();
        GroupedResourcesSource.Source = ViewModel.GroupedResources;
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        _suppressFilterEvents = true;
        ClearCheckboxes(TypeFilterList);
        ClearCheckboxes(LocationFilterList);
        _suppressFilterEvents = false;
        ViewModel.ClearFilters();
        GroupedResourcesSource.Source = ViewModel.GroupedResources;
    }

    private static void SyncCheckboxFilter(ItemsRepeater repeater, HashSet<string> target)
    {
        target.Clear();
        for (var i = 0; i < repeater.ItemsSourceView.Count; i++)
        {
            if (repeater.TryGetElement(i) is CheckBox cb && cb.IsChecked == true)
            {
                var item = repeater.ItemsSourceView.GetAt(i);
                var label = item is TypeFilterItem tfi ? tfi.DisplayName : item?.ToString() ?? "";
                target.Add(label);
            }
        }
    }

    private static void ClearCheckboxes(ItemsRepeater repeater)
    {
        for (var i = 0; i < repeater.ItemsSourceView.Count; i++)
        {
            if (repeater.TryGetElement(i) is CheckBox cb)
            {
                cb.IsChecked = false;
            }
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
        GroupedResourcesSource.Source = ViewModel.GroupedResources;
        var arrow = ViewModel.SortAscending ? "↑" : "↓";
        SortNameButton.Content = ViewModel.SortField == "Name" ? $"Name {arrow}" : "Name";
        SortTypeButton.Content = ViewModel.SortField == "Type" ? $"Type {arrow}" : "Type";
        SortLocationButton.Content = ViewModel.SortField == "Location" ? $"Location {arrow}" : "Location";
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
