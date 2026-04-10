using System.Collections.ObjectModel;
using AzureDesktop.Controls;
using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceGroupDetailPage : Page
{
    public ResourceGroupDetailViewModel ViewModel { get; }
    public ResourcesViewModel ResViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private NavigationContext? _navCtx;
    private BreadcrumbHelper? _breadcrumbHelper;
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

        if (e.Parameter is NavigationContext ctx && ctx.ResourceGroupName is not null)
        {
            _navCtx = ctx;

            var rgItem = new ResourceGroupItem(ctx.ResourceGroupName, ctx.ResourceGroupLocation ?? "");
            ViewModel.Load(ctx.SubscriptionId, rgItem);

            _breadcrumbHelper = new BreadcrumbHelper(Breadcrumb, EllipsisButton);
            _breadcrumbHelper.Add("Subscriptions", () => { Frame.BackStack.Clear(); Frame.Navigate(typeof(SubscriptionsPage)); });
            _breadcrumbHelper.Add("Subscription", () => Frame.Navigate(typeof(SubscriptionDetailPage), ctx.Subscription));
            _breadcrumbHelper.Add("Resource Group", () => { });
            _breadcrumbHelper.Apply();

            await ResViewModel.LoadForResourceGroupAsync(
                ctx.SubscriptionId, ctx.SubscriptionName, ctx.ResourceGroupName, default);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        _breadcrumbHelper?.HandleClick(args.Index);
    }

    private void Resource_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border { Tag: ResourceItem item } && _navCtx is not null)
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
    }

    private void LocationFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressFilterEvents) return;
        SyncCheckboxFilter(LocationFilterList, ResViewModel.SelectedLocations);
        ResViewModel.OnFilterChanged();
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        _suppressFilterEvents = true;
        ClearCheckboxes(TypeFilterList);
        ClearCheckboxes(LocationFilterList);
        _suppressFilterEvents = false;
        ResViewModel.ClearFilters();
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
        var arrow = ResViewModel.SortAscending ? "↑" : "↓";
        SortNameButton.Content = ResViewModel.SortField == "Name" ? $"Name {arrow}" : "Name";
        SortTypeButton.Content = ResViewModel.SortField == "Type" ? $"Type {arrow}" : "Type";
        SortLocationButton.Content = ResViewModel.SortField == "Location" ? $"Location {arrow}" : "Location";
    }

    private async void ManageTags_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ResourceId is not null)
        {
            await TagManagerDialog.ShowAsync(ViewModel.ResourceId, XamlRoot);
        }
    }

    private async void ManageLocks_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ResourceId is not null)
        {
            await LockManagerDialog.ShowAsync(ViewModel.ResourceId, XamlRoot);
        }
    }

    private void ResourceTile_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"];
        }
    }

    private void ResourceTile_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        }
    }
}
