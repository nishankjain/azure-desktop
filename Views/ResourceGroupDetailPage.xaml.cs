using System.Collections.ObjectModel;
using AzureDesktop.Controls;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceGroupDetailPage : Page
{
    private CancellationTokenSource? _cts;
    public ResourceGroupDetailViewModel ViewModel { get; }
    public ResourcesViewModel ResViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private NavigationContext? _navCtx;
    private bool _suppressFilterEvents;

    public ResourceGroupDetailPage()
    {
        ViewModel = App.GetService<ResourceGroupDetailViewModel>();
        ResViewModel = App.GetService<ResourcesViewModel>();
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

            var rgItem = new ResourceGroupItem(ctx.ResourceGroupName, ctx.ResourceGroupLocation ?? "");
            ViewModel.Load(ctx.SubscriptionId, rgItem);

            await ResViewModel.LoadForResourceGroupAsync(
                ctx.SubscriptionId, ctx.SubscriptionName, ctx.ResourceGroupName, _cts.Token);

            GroupedResourcesSource.Source = ResViewModel.GroupedResources;
        }
    }

    private void Resource_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ResourceItem item && _navCtx is not null)
        {
            var ctx = _navCtx with { Resource = item };
            Frame.Navigate(typeof(ResourceDetailPage), ctx);
        }
    }

    private void TypeFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressFilterEvents) return;
        SyncCheckboxFilter(TypeFilterList, ResViewModel.SelectedTypes);
        ResViewModel.OnFilterChanged();
        RefreshGroupedSource();
    }

    private void LocationFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressFilterEvents) return;
        SyncCheckboxFilter(LocationFilterList, ResViewModel.SelectedLocations);
        ResViewModel.OnFilterChanged();
        RefreshGroupedSource();
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        _suppressFilterEvents = true;
        ClearCheckboxes(TypeFilterList);
        ClearCheckboxes(LocationFilterList);
        _suppressFilterEvents = false;
        ResViewModel.ClearFilters();
        RefreshGroupedSource();
    }

    private static void SyncCheckboxFilter(Microsoft.UI.Xaml.Controls.ItemsRepeater repeater, HashSet<string> target)
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

    private void SortByName_Click(object sender, RoutedEventArgs e)
    {
        ResViewModel.ToggleSort("Name");
        UpdateSortButtons();
    }

    private void SortByType_Click(object sender, RoutedEventArgs e)
    {
        ResViewModel.ToggleSort("Type");
        UpdateSortButtons();
    }

    private void SortByLocation_Click(object sender, RoutedEventArgs e)
    {
        ResViewModel.ToggleSort("Location");
        UpdateSortButtons();
    }

    private void UpdateSortButtons()
    {
        RefreshGroupedSource();
        var arrow = ResViewModel.SortAscending ? "↑" : "↓";
        SortNameButton.Content = ResViewModel.SortField == "Name" ? $"Name {arrow}" : "Name";
        SortTypeButton.Content = ResViewModel.SortField == "Type" ? $"Type {arrow}" : "Type";
        SortLocationButton.Content = ResViewModel.SortField == "Location" ? $"Location {arrow}" : "Location";
    }

    private void RefreshGroupedSource()
    {
        GroupedResourcesSource.Source = ResViewModel.GroupedResources;
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
