using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceDetailPage : Page
{
    public ResourceDetailViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private NavigationContext? _navCtx;

    public ResourceDetailPage()
    {
        ViewModel = App.GetService<ResourceDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is NavigationContext ctx && ctx.Resource is not null)
        {
            _navCtx = ctx;
            BreadcrumbItems.Clear();
            BreadcrumbItems.Add("Subscriptions");
            BreadcrumbItems.Add(ctx.SubscriptionName);
            BreadcrumbItems.Add("Resource Groups");
            BreadcrumbItems.Add(ctx.ResourceGroupName ?? "");
            BreadcrumbItems.Add("Resources");
            BreadcrumbItems.Add(ctx.Resource.Name);
            Breadcrumb.ItemsSource = BreadcrumbItems;

            _ = ViewModel.LoadAsync(ctx.Resource);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (_navCtx is null) return;

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
                Frame.Navigate(typeof(ResourceGroupsPage), new NavigationContext(_navCtx.Subscription));
                break;
            case 3:
                Frame.Navigate(typeof(ResourceGroupDetailPage), _navCtx with { Resource = null });
                break;
            case 4:
                Frame.Navigate(typeof(ResourcesPage), _navCtx with { Resource = null });
                break;
        }
    }
}
