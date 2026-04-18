using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceGroupsPage : Page
{
    private CancellationTokenSource? _cts;
    public ResourceGroupsViewModel ViewModel { get; }

    private SubscriptionItem? _sub;
    private bool _suppressFilterEvents;

    public ResourceGroupsPage()
    {
        ViewModel = App.GetService<ResourceGroupsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (e.Parameter is NavigationContext ctx)
        {
            _sub = ctx.Subscription;
            await ViewModel.LoadForSubscriptionAsync(ctx.Subscription, _cts.Token);
        }
    }

    private void ResourceGroupList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ResourceGroupItem rgItem && _sub is not null)
        {
            Frame.Navigate(typeof(ResourceGroupDetailPage), new NavigationContext(_sub, rgItem.Name, rgItem.Location));
        }
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

    private void SortByName_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleSort("Name");
        var arrow = ViewModel.SortAscending ? "↑" : "↓";
        SortNameButton.Content = $"Name {arrow}";
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
