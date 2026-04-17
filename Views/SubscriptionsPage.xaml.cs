using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class SubscriptionsPage : Page
{
    private CancellationTokenSource? _cts;
    public SubscriptionsViewModel ViewModel { get; }

    public SubscriptionsPage()
    {
        ViewModel = App.GetService<SubscriptionsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (ViewModel.FilteredSubscriptions.Count == 0 && !ViewModel.IsLoading)
        {
            await ViewModel.LoadSubscriptionsCommand.ExecuteAsync(null);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }

    private void SubscriptionList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SubscriptionItem item)
        {
            Frame.Navigate(typeof(SubscriptionDetailPage), new NavigationContext(item));
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
