using System.Collections.ObjectModel;
using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceDetailPage : Page
{
    public ResourceDetailViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private NavigationContext? _navCtx;
    private BreadcrumbHelper? _breadcrumbHelper;

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

            _breadcrumbHelper = new BreadcrumbHelper(Breadcrumb, EllipsisButton);
            _breadcrumbHelper.Add("Subscriptions", () => { Frame.BackStack.Clear(); Frame.Navigate(typeof(SubscriptionsPage)); });
            _breadcrumbHelper.Add("Subscription", () => Frame.Navigate(typeof(SubscriptionDetailPage), ctx.Subscription));
            _breadcrumbHelper.Add("Resource Group", () => Frame.Navigate(typeof(ResourceGroupDetailPage), ctx with { Resource = null }));
            _breadcrumbHelper.Add(ctx.Resource.SingularType, () => { });
            _breadcrumbHelper.Apply();

            ResourceIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri(ctx.Resource.IconPath));

            _ = ViewModel.LoadAsync(ctx.Resource);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        _breadcrumbHelper?.HandleClick(args.Index);
    }
}
