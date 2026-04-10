using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class FeatureDetailPage : Page
{
    public FeatureDetailViewModel ViewModel { get; }

    private BreadcrumbHelper? _breadcrumbHelper;
    private SubscriptionItem? _subItem;

    public FeatureDetailPage()
    {
        ViewModel = App.GetService<FeatureDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is (FeatureEntry entry, SubscriptionItem sub))
        {
            _subItem = sub;
            ViewModel.Load(entry, sub.Id);

            _breadcrumbHelper = new BreadcrumbHelper(Breadcrumb, EllipsisButton);
            _breadcrumbHelper.Add("Subscriptions", () => { Frame.BackStack.Clear(); Frame.Navigate(typeof(SubscriptionsPage)); });
            _breadcrumbHelper.Add("Subscription", () => Frame.Navigate(typeof(SubscriptionDetailPage), _subItem));
            _breadcrumbHelper.Add("Preview Features", () => Frame.Navigate(typeof(FeaturesPage), _subItem));
            _breadcrumbHelper.Add("Feature", () => { });
            _breadcrumbHelper.Apply();
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        _breadcrumbHelper?.HandleClick(args.Index);
    }

    private async void ToggleRegistration_Click(object sender, RoutedEventArgs e)
    {
        if (_subItem is not null)
        {
            await ViewModel.ToggleRegistrationAsync(_subItem.Id);
        }
    }
}
