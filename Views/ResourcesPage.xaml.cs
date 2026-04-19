using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class ResourcesPage : NavigablePage
{
    public override string PageLabel => "Resources";
    public override string? ActiveNavTag => "Resources";
    public override NavItemDefinition[] GetNavItems() => ResourceGroupNavItems.Get();

    public ResourcesViewModel ViewModel { get; }
    private bool _suppressFilterEvents;

    public ResourcesPage()
    {
        ViewModel = App.GetService<ResourcesViewModel>();
        InitializeComponent();
    }

    protected override async void OnContextReady(NavigationContext? ctx)
    {
        if (ctx?.ResourceGroupName is not null)
        {
            await ViewModel.LoadForResourceGroupAsync(
                ctx.SubscriptionId, ctx.SubscriptionName, ctx.ResourceGroupName, Cts!.Token);

            GroupedResourcesSource.Source = ViewModel.GroupedResources;
        }
    }

    private void Resource_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ResourceItem item && NavCtx is not null)
        {
            Frame.Navigate(typeof(ResourceDetailPage), NavCtx with { Resource = item });
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
}
