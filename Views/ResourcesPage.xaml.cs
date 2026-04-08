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
            BreadcrumbItems.Add("Resource Groups");
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
                var rgListCtx = new NavigationContext(_navCtx.Subscription);
                Frame.Navigate(typeof(ResourceGroupsPage), rgListCtx);
                break;
            case 3:
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

    private void TypeFilterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressFilterEvents)
        {
            return;
        }

        ViewModel.SelectedTypes.Clear();
        foreach (string item in TypeFilterList.SelectedItems)
        {
            ViewModel.SelectedTypes.Add(item);
        }

        ViewModel.OnFilterChanged();
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
        TypeFilterList.SelectedItems.Clear();
        LocationFilterList.SelectedItems.Clear();
        _suppressFilterEvents = false;
        ViewModel.ClearFilters();
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
