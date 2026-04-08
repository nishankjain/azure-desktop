using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class SubscriptionDetailPage : Page
{
    public SubscriptionDetailViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    public SubscriptionDetailPage()
    {
        ViewModel = App.GetService<SubscriptionDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is SubscriptionItem item)
        {
            ViewModel.Subscription = item;
            BreadcrumbItems.Clear();
            BreadcrumbItems.Add("Subscriptions");
            BreadcrumbItems.Add(item.Name);
            Breadcrumb.ItemsSource = BreadcrumbItems;
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index == 0)
        {
            Frame.BackStack.Clear();
            Frame.Navigate(typeof(SubscriptionsPage));
        }
    }
}
