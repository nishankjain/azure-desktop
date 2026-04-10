using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceProvidersPage : Page
{
    public ResourceProvidersViewModel ViewModel { get; }

    private BreadcrumbHelper? _breadcrumbHelper;
    private SubscriptionItem? _subItem;

    public ResourceProvidersPage()
    {
        ViewModel = App.GetService<ResourceProvidersViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is SubscriptionItem sub)
        {
            _subItem = sub;
            _breadcrumbHelper = new BreadcrumbHelper(Breadcrumb, EllipsisButton);
            _breadcrumbHelper.Add("Subscriptions", () => { Frame.BackStack.Clear(); Frame.Navigate(typeof(SubscriptionsPage)); });
            _breadcrumbHelper.Add("Subscription", () => Frame.Navigate(typeof(SubscriptionDetailPage), _subItem));
            _breadcrumbHelper.Add("Resource Providers", () => { });
            _breadcrumbHelper.Apply();

            await ViewModel.LoadProvidersAsync(sub.Id);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        _breadcrumbHelper?.HandleClick(args.Index);
    }

    private void Provider_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ResourceProviderEntry entry && _subItem is not null)
        {
            Frame.Navigate(typeof(ResourceProviderDetailPage), (entry, _subItem));
        }
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ResourceProviderEntry entry })
        {
            await ViewModel.RegisterProviderAsync(entry);
        }
    }

    private async void Unregister_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ResourceProviderEntry entry })
        {
            await ViewModel.UnregisterProviderAsync(entry);
        }
    }

    private void SortToggle_Click(object sender, RoutedEventArgs e)
    {
        SortLabel.Text = ViewModel.SortAscending ? "A-Z" : "Z-A";
    }
}
