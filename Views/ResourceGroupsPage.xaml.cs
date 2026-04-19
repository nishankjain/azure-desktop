using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class ResourceGroupsPage : NavigablePage
{
    public override string PageLabel => "Resource Groups";
    public override string? ActiveNavTag => "ResourceGroups";
    public override NavItemDefinition[] GetNavItems() => SubscriptionNavItems.Get();

    public ResourceGroupsViewModel ViewModel { get; }

    private SubscriptionItem? _sub;
    private bool _suppressFilterEvents;

    public ResourceGroupsPage()
    {
        ViewModel = App.GetService<ResourceGroupsViewModel>();
        InitializeComponent();
    }

    protected override async void OnContextReady(NavigationContext? ctx)
    {
        if (ctx is not null)
        {
            _sub = ctx.Subscription;
            await ViewModel.LoadForSubscriptionAsync(ctx.Subscription, Cts!.Token);
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
}
