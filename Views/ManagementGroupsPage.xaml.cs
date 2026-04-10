using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class ManagementGroupsPage : Page
{
    public ManagementGroupsViewModel ViewModel { get; }

    public ManagementGroupsPage()
    {
        ViewModel = App.GetService<ManagementGroupsViewModel>();
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ManagementGroups.Count == 0 && ViewModel.FlatSubscriptions.Count == 0)
        {
            await ViewModel.LoadCommand.ExecuteAsync(null);
        }
    }

    private void ManagementGroupList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ManagementGroupItem item)
        {
            Frame.Navigate(typeof(SubscriptionsPage), item);
        }
    }

    private void SubscriptionList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SubscriptionItem item)
        {
            Frame.Navigate(typeof(SubscriptionDetailPage), item);
        }
    }
}
