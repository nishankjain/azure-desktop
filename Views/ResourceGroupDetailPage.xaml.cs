using System.Collections.ObjectModel;
using AzureDesktop.Controls;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceGroupDetailPage : Page
{
    public ResourceGroupDetailViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private NavigationContext? _navCtx;

    public ResourceGroupDetailPage()
    {
        ViewModel = App.GetService<ResourceGroupDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is NavigationContext ctx && ctx.ResourceGroupName is not null)
        {
            _navCtx = ctx;

            var rgItem = new ResourceGroupItem(ctx.ResourceGroupName, ctx.ResourceGroupLocation ?? "");
            ViewModel.Load(ctx.SubscriptionId, rgItem);

            BreadcrumbItems.Clear();
            BreadcrumbItems.Add("Subscriptions");
            BreadcrumbItems.Add(ctx.SubscriptionName);
            BreadcrumbItems.Add("Resource Groups");
            BreadcrumbItems.Add(ctx.ResourceGroupName);
            Breadcrumb.ItemsSource = BreadcrumbItems;
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
        }
    }

    private void ViewResources_Click(object sender, RoutedEventArgs e)
    {
        if (_navCtx is not null)
        {
            Frame.Navigate(typeof(ResourcesPage), _navCtx);
        }
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
}
