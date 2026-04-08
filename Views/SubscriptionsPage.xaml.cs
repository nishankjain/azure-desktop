using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class SubscriptionsPage : Page
{
    public SubscriptionsViewModel ViewModel { get; }

    public SubscriptionsPage()
    {
        ViewModel = App.GetService<SubscriptionsViewModel>();
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.FilteredSubscriptions.Count == 0 && !ViewModel.IsLoading)
        {
            await ViewModel.LoadSubscriptionsCommand.ExecuteAsync(null);
        }
    }

    private void SubscriptionList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SubscriptionItem item)
        {
            Frame.Navigate(typeof(SubscriptionDetailPage), item);
        }
    }

    private void SortByName_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleSort("Name");
        UpdateSortButtons();
    }

    private void SortById_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleSort("Id");
        UpdateSortButtons();
    }

    private void UpdateSortButtons()
    {
        var arrow = ViewModel.SortAscending ? "↑" : "↓";
        SortNameButton.Content = ViewModel.SortField == "Name" ? $"Name {arrow}" : "Name";
        SortIdButton.Content = ViewModel.SortField == "Id" ? $"ID {arrow}" : "ID";
    }
}
